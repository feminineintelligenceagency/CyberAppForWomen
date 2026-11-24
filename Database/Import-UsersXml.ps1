<#
.SYNOPSIS
    One-time import of App_Data\users.xml into the Users and UserCredentials tables.

.DESCRIPTION
    - Reads users.xml (never modifies it).
    - Inserts new users, and updates users that were already imported, so it is
      safe to re-run until the app switches over to the database.
    - Keeps existing password hashes as-is: PBKDF2 accounts are stored with
      Algorithm = 'pbkdf2' and still upgrade to Argon2 at their next login.
    - Runs in a single transaction: if anything fails, nothing is written.
    - Skips (and reports) records that are invalid or have duplicate emails.

.EXAMPLE
    # Dry run against LocalDB: reports what would happen, then rolls back
    .\Import-UsersXml.ps1 -XmlPath "C:\dev\CyberAppForWomen\App_Data\users.xml" `
        -ConnectionString "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CyberAppDb;Integrated Security=True" `
        -DryRun

.EXAMPLE
    # Real import
    .\Import-UsersXml.ps1 -XmlPath "C:\dev\CyberAppForWomen\App_Data\users.xml" `
        -ConnectionString "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CyberAppDb;Integrated Security=True"
#>
param(
    [Parameter(Mandatory = $true)] [string] $XmlPath,
    [Parameter(Mandatory = $true)] [string] $ConnectionString,
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data

# Canonical role spellings (lookup is case-insensitive and trims spaces).
$Roles = @{
    'participant'     = 'Participant'
    'helper'          = 'Helper'
    'universityadmin' = 'UniversityAdmin'
    'superadmin'      = 'SuperAdmin'
}

function Get-ChildText([System.Xml.XmlNode] $node, [string] $name) {
    $el = $node.SelectSingleNode($name)
    if ($null -eq $el) { return '' }
    return $el.InnerText.Trim()
}

function ConvertTo-UtcOrNull([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return [DBNull]::Value }
    $styles = [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal
    return [DateTime]::Parse($value, [Globalization.CultureInfo]::InvariantCulture, $styles)
}

function ConvertTo-IntOrNull([string] $value) {
    $n = 0
    if ([int]::TryParse($value, [ref] $n)) { return $n }
    return [DBNull]::Value
}

function Add-Param($cmd, [string] $name, [System.Data.SqlDbType] $type, [int] $size, $value) {
    $p = $cmd.Parameters.Add($name, $type)
    if ($size -gt 0) { $p.Size = $size }
    if ($null -eq $value) { $value = [DBNull]::Value }
    $p.Value = $value
}

# ---------------------------------------------------------------------------
# 1. Read and validate users.xml
# ---------------------------------------------------------------------------
$doc = New-Object System.Xml.XmlDocument
$doc.Load((Resolve-Path -LiteralPath $XmlPath).Path)

$userNodes = $doc.SelectNodes('/users/user')
$valid = New-Object System.Collections.Generic.List[object]
$seenEmails = @{}
$skipped = 0

foreach ($u in $userNodes) {
    $id    = ($u.GetAttribute('id')).Trim().ToLowerInvariant()
    $email = (Get-ChildText $u 'email').ToLowerInvariant()
    $label = if ($email) { $email } else { "(id $id)" }

    $roleKey = ($u.GetAttribute('role')).Trim().ToLowerInvariant()
    if (-not $Roles.ContainsKey($roleKey)) {
        Write-Warning "Skipping ${label}: unknown role '$($u.GetAttribute('role'))'."; $skipped++; continue
    }
    if ($id -notmatch '^[0-9a-f]{32}$') {
        Write-Warning "Skipping ${label}: id '$id' is not a 32-character hex id."; $skipped++; continue
    }
    if (-not $email) {
        Write-Warning "Skipping user ${id}: no email."; $skipped++; continue
    }
    if ($seenEmails.ContainsKey($email)) {
        Write-Warning "Skipping ${label} (id $id): duplicate email, already used by id $($seenEmails[$email])."; $skipped++; continue
    }

    # Accept the lowercase names (correct) and the capitalized names an earlier
    # buggy version of passHasher wrote for some test accounts.
    $hashEl = $u.SelectSingleNode('passwordHash')
    if ($null -eq $hashEl) { $hashEl = $u.SelectSingleNode('PasswordHash') }
    $saltText = Get-ChildText $u 'passwordSalt'
    if (-not $saltText) { $saltText = Get-ChildText $u 'PasswordSalt' }

    if ($null -eq $hashEl -or -not $hashEl.InnerText.Trim() -or -not $saltText) {
        Write-Warning "Skipping ${label}: missing password hash or salt."; $skipped++; continue
    }

    try {
        $hashBytes = [Convert]::FromBase64String($hashEl.InnerText.Trim())
        $saltBytes = [Convert]::FromBase64String($saltText)
    } catch {
        Write-Warning "Skipping ${label}: password hash or salt is not valid Base64."; $skipped++; continue
    }

    # No algo attribute = legacy PBKDF2 account.
    $algo = $hashEl.GetAttribute('algo').Trim()
    if (-not $algo) { $algo = 'pbkdf2' }

    $isPbkdf2 = $algo -ieq 'pbkdf2'
    $m = if ($isPbkdf2) { [DBNull]::Value } else { ConvertTo-IntOrNull $hashEl.GetAttribute('m') }
    $t = if ($isPbkdf2) { [DBNull]::Value } else { ConvertTo-IntOrNull $hashEl.GetAttribute('t') }
    $p = if ($isPbkdf2) { [DBNull]::Value } else { ConvertTo-IntOrNull $hashEl.GetAttribute('p') }
    if (-not $isPbkdf2 -and ($m -is [DBNull] -or $t -is [DBNull] -or $p -is [DBNull])) {
        Write-Warning "Skipping ${label}: Argon2 hash is missing its m/t/p settings."; $skipped++; continue
    }

    $created = ConvertTo-UtcOrNull (Get-ChildText $u 'createdAt')
    if ($created -is [DBNull]) { $created = [DateTime]::UtcNow }

    $university = Get-ChildText $u 'university'
    $consentIp  = Get-ChildText $u 'consentIp'

    $seenEmails[$email] = $id
    $valid.Add([pscustomobject]@{
        Id         = $id
        Email      = $email
        FirstName  = Get-ChildText $u 'firstName'
        LastName   = Get-ChildText $u 'lastName'
        Role       = $Roles[$roleKey]
        University = if ($university) { $university } else { [DBNull]::Value }
        CreatedAt  = $created
        ConsentAt  = ConvertTo-UtcOrNull (Get-ChildText $u 'consentAcceptedAt')
        ConsentIp  = if ($consentIp) { $consentIp } else { [DBNull]::Value }
        Hash       = $hashBytes
        Salt       = $saltBytes
        Algo       = $algo
        M          = $m
        T          = $t
        P          = $p
    })
}

Write-Host "Found $($userNodes.Count) user(s) in users.xml: $($valid.Count) valid, $skipped skipped."
if ($valid.Count -eq 0) { Write-Host 'Nothing to import.'; return }

# ---------------------------------------------------------------------------
# 2. Write to the database in one transaction
# ---------------------------------------------------------------------------
$upsertSql = @'
IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @Id)
BEGIN
    UPDATE dbo.Users
       SET Email = @Email, FirstName = @FirstName, LastName = @LastName, Role = @Role,
           University = @University, CreatedAtUtc = @CreatedAt,
           ConsentAcceptedAtUtc = @ConsentAt, ConsentIp = @ConsentIp
     WHERE UserId = @Id;
    SELECT 'updated';
END
ELSE
BEGIN
    INSERT INTO dbo.Users (UserId, Email, FirstName, LastName, Role, University,
                           CreatedAtUtc, ConsentAcceptedAtUtc, ConsentIp)
    VALUES (@Id, @Email, @FirstName, @LastName, @Role, @University,
            @CreatedAt, @ConsentAt, @ConsentIp);
    SELECT 'inserted';
END

IF EXISTS (SELECT 1 FROM dbo.UserCredentials WHERE UserId = @Id)
    UPDATE dbo.UserCredentials
       SET PasswordHash = @Hash, PasswordSalt = @Salt, Algorithm = @Algo,
           Argon2MemoryKiB = @M, Argon2Iterations = @T, Argon2Parallelism = @P
     WHERE UserId = @Id;
ELSE
    INSERT INTO dbo.UserCredentials (UserId, PasswordHash, PasswordSalt, Algorithm,
                                     Argon2MemoryKiB, Argon2Iterations, Argon2Parallelism)
    VALUES (@Id, @Hash, @Salt, @Algo, @M, @T, @P);
'@

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()
$tx = $conn.BeginTransaction()
$inserted = 0; $updated = 0

try {
    foreach ($r in $valid) {
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = $upsertSql

        Add-Param $cmd '@Id'         Char      32  $r.Id
        Add-Param $cmd '@Email'      NVarChar  256 $r.Email
        Add-Param $cmd '@FirstName'  NVarChar  100 $r.FirstName
        Add-Param $cmd '@LastName'   NVarChar  100 $r.LastName
        Add-Param $cmd '@Role'       VarChar   20  $r.Role
        Add-Param $cmd '@University' NVarChar  200 $r.University
        Add-Param $cmd '@CreatedAt'  DateTime2 0   $r.CreatedAt
        Add-Param $cmd '@ConsentAt'  DateTime2 0   $r.ConsentAt
        Add-Param $cmd '@ConsentIp'  VarChar   45  $r.ConsentIp
        Add-Param $cmd '@Hash'       VarBinary 64  $r.Hash
        Add-Param $cmd '@Salt'       VarBinary 64  $r.Salt
        Add-Param $cmd '@Algo'       VarChar   20  $r.Algo
        Add-Param $cmd '@M'          Int       0   $r.M
        Add-Param $cmd '@T'          Int       0   $r.T
        Add-Param $cmd '@P'          Int       0   $r.P

        $result = $cmd.ExecuteScalar()
        if ($result -eq 'inserted') { $inserted++ } else { $updated++ }
        $cmd.Dispose()
    }

    if ($DryRun) {
        $tx.Rollback()
        Write-Host "DRY RUN: would insert $inserted and update $updated user(s). Nothing was saved."
    } else {
        $tx.Commit()
        Write-Host "Done: inserted $inserted, updated $updated user(s)."
    }
}
catch {
    $tx.Rollback()
    Write-Error "Import failed and was rolled back. Nothing was saved. $($_.Exception.Message)"
}
finally {
    $conn.Close()
}
