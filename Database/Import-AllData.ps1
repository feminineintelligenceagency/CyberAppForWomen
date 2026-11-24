<#
.SYNOPSIS
    Copies the app's XML data (App_Data) into CyberAppDB.

.DESCRIPTION
    Imports, in the right order:
      1. users.xml                        -> Users, UserCredentials
      2. Batch 1 (audit log, courses, events)
      3. Batch 2 (quiz rulesets, questions, progress, results)

    Safe to run more than once: rows that already exist are updated, not duplicated.
    Use -DryRun first to check everything without saving anything.

    Keep this script in the same folder as:
      Import-UsersXml.ps1, Import-Batch1.ps1, Import-Batch2.ps1

.PARAMETER AppDataPath
    The project's App_Data folder (the one containing users.xml).
    Defaults to ..\App_Data next to this Database folder.

.PARAMETER ConnectionString
    Defaults to the local database used in Web.config. If your LocalDB is called
    MSSQLLocalDB instead of ProjectModels, pass the connection string yourself.

.EXAMPLE
    # From the project folder (these scripts live in CyberAppForWomen\Database):
    powershell -ExecutionPolicy Bypass -File .\Database\Import-AllData.ps1 -DryRun
    powershell -ExecutionPolicy Bypass -File .\Database\Import-AllData.ps1

.EXAMPLE
    > open powershell as admin
    > cd "???" (path to where import-all.ps1 is downloaded)
    > powershell -ExecutionPolicy Bypass -File .\Import-AllData.ps1 -AppDataPath "???" -DryRun
	(repace ??? with path to CyberAppForWomen\App_Data)
	
	IF WORKS, RUN AGAIN WITHOUT "-DryRun"
	

#>
param(
    [string] $AppDataPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'App_Data'),
    [string] $ConnectionString = 'Data Source=(localdb)\ProjectModels;Initial Catalog=CyberAppDB;Integrated Security=True',
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$here = $PSScriptRoot

function Write-Step([string] $text) { Write-Host ""; Write-Host "=== $text ===" -ForegroundColor Cyan }

# ---------------------------------------------------------------------------
# Check everything is in place before importing anything
# ---------------------------------------------------------------------------
foreach ($f in 'Import-UsersXml.ps1', 'Import-Batch1.ps1', 'Import-Batch2.ps1') {
    if (-not (Test-Path -LiteralPath (Join-Path $here $f))) {
        throw "Missing $f. Put it in the same folder as Import-AllData.ps1."
    }
}

$AppDataPath = $AppDataPath.TrimEnd('\', '/')
if (-not (Test-Path -LiteralPath (Join-Path $AppDataPath 'users.xml'))) {
    throw "users.xml not found in '$AppDataPath'. -AppDataPath should be the project's App_Data folder."
}

# Can we reach the database, and were the tables created?
$requiredTables = 'Users', 'UserCredentials', 'AuditLog', 'Microcourses', 'Events',
                  'QuizRulesets', 'QuizProgress', 'QuizResults', 'UserMfaBackupCodes', 'PasswordResetTokens'
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
Write-Step "Checking database '$($builder['Initial Catalog'])' on $($builder['Data Source'])"

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
try {
    $conn.Open()
} catch {
    throw "Couldn't connect to the database. Did SQLQuery1.sql (CreateCyberAppDB_AllTables.sql) run on this server? ($($_.Exception.Message))"
}
try {
    $missing = @()
    foreach ($t in $requiredTables) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT OBJECT_ID(N'dbo.' + @T, N'U')"
        [void]$cmd.Parameters.AddWithValue('@T', $t)
        if ($cmd.ExecuteScalar() -is [DBNull]) { $missing += $t }
    }
    if ($missing.Count -gt 0) {
        throw "These tables are missing: $($missing -join ', '). Run SQLQuery1.sql (in the project folder) first."
    }
    Write-Host "Connected. All tables found."
} finally { $conn.Close() }

# ---------------------------------------------------------------------------
# Import, in dependency order (users first: quiz data references them)
# ---------------------------------------------------------------------------
Write-Step 'Importing users.xml'
& (Join-Path $here 'Import-UsersXml.ps1') -XmlPath (Join-Path $AppDataPath 'users.xml') `
    -ConnectionString $ConnectionString -DryRun:$DryRun

Write-Step 'Importing Batch 1 (audit log, microcourses, events)'
& (Join-Path $here 'Import-Batch1.ps1') -AppDataPath $AppDataPath -ConnectionString $ConnectionString -DryRun:$DryRun

Write-Step 'Importing Batch 2 (quiz)'
& (Join-Path $here 'Import-Batch2.ps1') -AppDataPath $AppDataPath -ConnectionString $ConnectionString -DryRun:$DryRun

# ---------------------------------------------------------------------------
# Summary: row counts so you can see what was imported
# ---------------------------------------------------------------------------
Write-Step 'Row counts'
$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()
try {
    foreach ($t in 'Users', 'UserCredentials', 'AuditLog', 'Microcourses', 'Events',
                   'QuizRulesets', 'QuizQuestions', 'QuizProgress', 'QuizResults') {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT COUNT(*) FROM dbo.[$t]"
        '{0,-18} {1,6}' -f $t, $cmd.ExecuteScalar() | Write-Host
    }
} finally { $conn.Close() }

Write-Host ""
if ($DryRun) {
    Write-Host 'DRY RUN finished: nothing was saved. Run again without -DryRun to import.' -ForegroundColor Yellow
} else {
    Write-Host 'Data import complete.' -ForegroundColor Green
}
