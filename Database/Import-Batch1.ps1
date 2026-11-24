<#
.SYNOPSIS
    One-time import of Batch 1 XML files into the database:
      Audit_Log\UnvAdminAudit.xml -> AuditLog
      microcourses.xml            -> Microcourses, MicrocourseTags,
                                     MicrocourseRequiredRules, MicrocoursePrerequisites
      events.xml                  -> Events
      eventCourses.xml            -> EventCourses

.DESCRIPTION
    - Read-only on the XML files.
    - Safe to re-run: courses and events are updated, their tag/rule/course
      lists are replaced, and audit entries already imported are left alone.
    - One transaction: if anything fails, nothing is saved.
    - Skips (and reports) bad records, including event-course links that point
      to events or courses that don't exist.

.EXAMPLE
    & "C:\path\Import-Batch1.ps1" -AppDataPath "C:\path\App_Data" `
      -ConnectionString "Data Source=(localdb)\ProjectModels;Initial Catalog=CyberAppDB;Integrated Security=True" -DryRun
#>
param(
    [Parameter(Mandatory = $true)] [string] $AppDataPath,
    [Parameter(Mandatory = $true)] [string] $ConnectionString,
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Get-XmlFile([string] $relativePath) {
    $path = Join-Path $AppDataPath $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Warning "$relativePath not found; skipping it."
        return $null
    }
    $doc = New-Object System.Xml.XmlDocument
    $doc.Load((Resolve-Path -LiteralPath $path).Path)
    return ,$doc   # leading comma stops PowerShell from unrolling the XmlDocument
}

function Get-ChildText([System.Xml.XmlNode] $node, [string] $name) {
    $el = $node.SelectSingleNode($name)
    if ($null -eq $el) { return '' }
    return $el.InnerText.Trim()
}

function ConvertTo-UtcOrNull([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return [DBNull]::Value }
    $styles = [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal
    return [DateTime]::Parse($value.Trim(), [Globalization.CultureInfo]::InvariantCulture, $styles)
}

function ConvertTo-IntOrNull([string] $value) {
    $n = 0
    if ([int]::TryParse($value.Trim(), [ref] $n)) { return $n }
    return [DBNull]::Value
}

function OrNull([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return [DBNull]::Value }
    return $value.Trim()
}

function Test-Id32([string] $id) { return $id -match '^[0-9a-f]{32}$' }

function Invoke-Sql([string] $sql, [hashtable] $params) {
    $cmd = $script:conn.CreateCommand()
    $cmd.Transaction = $script:tx
    $cmd.CommandText = $sql
    foreach ($key in $params.Keys) {
        $value = $params[$key]
        if ($null -eq $value) { $value = [DBNull]::Value }
        [void] $cmd.Parameters.AddWithValue($key, $value)
    }
    $result = $cmd.ExecuteScalar()
    $cmd.Dispose()
    return $result
}

$stats = [ordered]@{}
function Add-Stat([string] $name) { if ($stats.Contains($name)) { $stats[$name]++ } else { $stats[$name] = 1 } }

# ---------------------------------------------------------------------------
# Load XML
# ---------------------------------------------------------------------------
$auditDoc   = Get-XmlFile 'Audit_Log\UnvAdminAudit.xml'
$courseDoc  = Get-XmlFile 'microcourses.xml'
$eventDoc   = Get-XmlFile 'events.xml'
$evCourseDoc = Get-XmlFile 'eventCourses.xml'

$script:conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$script:conn.Open()
$script:tx = $script:conn.BeginTransaction()

try {
    # =======================================================================
    # Microcourses (first, because events and prerequisites reference them)
    # =======================================================================
    $courseIds = @{}      # course ids successfully imported this run
    $prereqMap = @{}      # courseId -> list of prerequisite course ids

    if ($courseDoc) {
        foreach ($c in $courseDoc.SelectNodes('/microcourses/course')) {
            $id = $c.GetAttribute('id').Trim().ToLowerInvariant()
            $title = Get-ChildText $c 'title'
            if (-not (Test-Id32 $id)) { Write-Warning "Course skipped: bad id '$id'."; Add-Stat 'Courses skipped'; continue }
            if (-not $title)          { Write-Warning "Course $id skipped: no title."; Add-Stat 'Courses skipped'; continue }

            $status = $c.GetAttribute('status').Trim()
            if (-not $status) { $status = 'Draft' }
            $created = ConvertTo-UtcOrNull $c.GetAttribute('createdAt')
            if ($created -is [DBNull]) { $created = [DateTime]::UtcNow }

            $p = @{
                '@Id' = $id; '@Title' = $title; '@Summary' = (OrNull (Get-ChildText $c 'summary'))
                '@Status' = $status
                '@Duration' = (ConvertTo-IntOrNull (Get-ChildText $c 'duration'))
                '@Link' = (OrNull (Get-ChildText $c 'externalLink'))
                '@Start' = (ConvertTo-UtcOrNull (Get-ChildText $c 'startTime'))
                '@End' = (ConvertTo-UtcOrNull (Get-ChildText $c 'endTime'))
                '@Max' = (ConvertTo-IntOrNull (Get-ChildText $c 'maxParticipants'))
                '@Created' = $created
                '@CreatedBy' = (OrNull $c.GetAttribute('createdBy'))
            }

            $result = Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.Microcourses WHERE CourseId = @Id)
BEGIN
    UPDATE dbo.Microcourses
       SET Title = @Title, Summary = @Summary, Status = @Status, DurationMinutes = @Duration,
           ExternalLink = @Link, StartTimeUtc = @Start, EndTimeUtc = @End,
           MaxParticipants = @Max, CreatedAtUtc = @Created, CreatedByEmail = @CreatedBy
     WHERE CourseId = @Id;
    SELECT 'updated';
END
ELSE
BEGIN
    INSERT INTO dbo.Microcourses (CourseId, Title, Summary, Status, DurationMinutes, ExternalLink,
                                  StartTimeUtc, EndTimeUtc, MaxParticipants, CreatedAtUtc, CreatedByEmail)
    VALUES (@Id, @Title, @Summary, @Status, @Duration, @Link, @Start, @End, @Max, @Created, @CreatedBy);
    SELECT 'inserted';
END
'@ $p
            Add-Stat "Courses $result"
            $courseIds[$id] = $true

            # Replace tags (case-insensitive de-duplication)
            [void](Invoke-Sql 'DELETE FROM dbo.MicrocourseTags WHERE CourseId = @Id' @{ '@Id' = $id })
            $seenTags = @{}
            foreach ($t in $c.SelectNodes('tags/tag')) {
                $tag = $t.InnerText.Trim()
                if (-not $tag -or $tag.Length -gt 50 -or $seenTags.ContainsKey($tag)) { continue }
                $seenTags[$tag] = $true
                [void](Invoke-Sql 'INSERT INTO dbo.MicrocourseTags (CourseId, Tag) VALUES (@Id, @Tag)' @{ '@Id' = $id; '@Tag' = $tag })
                Add-Stat 'Course tags'
            }

            # Replace required rules
            [void](Invoke-Sql 'DELETE FROM dbo.MicrocourseRequiredRules WHERE CourseId = @Id' @{ '@Id' = $id })
            $seenRules = @{}
            foreach ($r in $c.SelectNodes('requiredRules/rule')) {
                $ruleId = 0
                if (-not [int]::TryParse($r.GetAttribute('id').Trim(), [ref] $ruleId)) {
                    Write-Warning "Course ${id}: rule id '$($r.GetAttribute('id'))' is not a number; skipped."; continue
                }
                if ($seenRules.ContainsKey($ruleId)) { continue }
                $seenRules[$ruleId] = $true
                [void](Invoke-Sql 'INSERT INTO dbo.MicrocourseRequiredRules (CourseId, RuleId) VALUES (@Id, @Rule)' @{ '@Id' = $id; '@Rule' = $ruleId })
                Add-Stat 'Course required rules'
            }

            # Collect prerequisites; inserted after all courses exist.
            # Accepts <x id="..."/> or <x>id</x> for whatever element name is used.
            $list = New-Object System.Collections.Generic.List[string]
            foreach ($pr in $c.SelectNodes('prerequisites/*')) {
                $preId = $pr.GetAttribute('id').Trim()
                if (-not $preId) { $preId = $pr.InnerText.Trim() }
                $preId = $preId.ToLowerInvariant()
                if ($preId) { $list.Add($preId) }
            }
            $prereqMap[$id] = $list
        }

        # Prerequisites pass
        foreach ($cid in $prereqMap.Keys) {
            [void](Invoke-Sql 'DELETE FROM dbo.MicrocoursePrerequisites WHERE CourseId = @Id' @{ '@Id' = $cid })
            $seen = @{}
            foreach ($preId in $prereqMap[$cid]) {
                if ($seen.ContainsKey($preId)) { continue }
                $seen[$preId] = $true
                if ($preId -eq $cid -or -not $courseIds.ContainsKey($preId)) {
                    Write-Warning "Course ${cid}: prerequisite '$preId' is itself or not a known course; skipped."
                    Add-Stat 'Prerequisites skipped'; continue
                }
                [void](Invoke-Sql 'INSERT INTO dbo.MicrocoursePrerequisites (CourseId, PrerequisiteCourseId) VALUES (@Id, @Pre)' @{ '@Id' = $cid; '@Pre' = $preId })
                Add-Stat 'Prerequisites'
            }
        }
    }

    # =======================================================================
    # Events
    # =======================================================================
    $eventIds = @{}
    if ($eventDoc) {
        foreach ($e in $eventDoc.SelectNodes('/events/event')) {
            $id = $e.GetAttribute('id').Trim().ToLowerInvariant()
            $name = Get-ChildText $e 'name'
            if (-not (Test-Id32 $id)) { Write-Warning "Event skipped: bad id '$id'."; Add-Stat 'Events skipped'; continue }
            if (-not $name)           { Write-Warning "Event $id skipped: no name."; Add-Stat 'Events skipped'; continue }

            $start = ConvertTo-UtcOrNull (Get-ChildText $e 'startDate')
            $end   = ConvertTo-UtcOrNull (Get-ChildText $e 'endDate')
            if (-not ($start -is [DBNull]) -and -not ($end -is [DBNull]) -and $end -lt $start) {
                Write-Warning "Event '$name' ($id) skipped: end date is before start date."; Add-Stat 'Events skipped'; continue
            }

            $status = $e.GetAttribute('status').Trim()
            if (-not $status) { $status = 'Draft' }
            $created = ConvertTo-UtcOrNull $e.GetAttribute('createdAt')
            if ($created -is [DBNull]) { $created = [DateTime]::UtcNow }

            $p = @{
                '@Id' = $id; '@Name' = $name
                '@Desc' = (OrNull (Get-ChildText $e 'description'))
                '@Uni' = (OrNull (Get-ChildText $e 'university'))
                '@Status' = $status; '@Start' = $start; '@End' = $end
                '@Created' = $created; '@CreatedBy' = (OrNull $e.GetAttribute('createdBy'))
            }

            $result = Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.Events WHERE EventId = @Id)
BEGIN
    UPDATE dbo.Events
       SET Name = @Name, Description = @Desc, University = @Uni, Status = @Status,
           StartDateUtc = @Start, EndDateUtc = @End, CreatedAtUtc = @Created, CreatedByEmail = @CreatedBy
     WHERE EventId = @Id;
    SELECT 'updated';
END
ELSE
BEGIN
    INSERT INTO dbo.Events (EventId, Name, Description, University, Status,
                            StartDateUtc, EndDateUtc, CreatedAtUtc, CreatedByEmail)
    VALUES (@Id, @Name, @Desc, @Uni, @Status, @Start, @End, @Created, @CreatedBy);
    SELECT 'inserted';
END
'@ $p
            Add-Stat "Events $result"
            $eventIds[$id] = $true
        }
    }

    # =======================================================================
    # EventCourses
    # =======================================================================
    if ($evCourseDoc) {
        foreach ($ev in $evCourseDoc.SelectNodes('/eventCourses/event')) {
            $eid = $ev.GetAttribute('id').Trim().ToLowerInvariant()
            if (-not $eventIds.ContainsKey($eid)) {
                Write-Warning "eventCourses.xml: event $eid is not in events.xml; its course list was skipped."
                Add-Stat 'Event-course lists skipped (unknown event)'; continue
            }

            [void](Invoke-Sql 'DELETE FROM dbo.EventCourses WHERE EventId = @E' @{ '@E' = $eid })
            $seen = @{}
            foreach ($co in $ev.SelectNodes('course')) {
                $cid = $co.GetAttribute('id').Trim().ToLowerInvariant()
                if ($seen.ContainsKey($cid)) { continue }
                $seen[$cid] = $true
                if (-not $courseIds.ContainsKey($cid)) {
                    Write-Warning "Event ${eid}: course $cid is not in microcourses.xml; skipped."
                    Add-Stat 'Event-course links skipped (unknown course)'; continue
                }
                $enabled = $co.GetAttribute('enabled').Trim() -ieq 'true'
                [void](Invoke-Sql 'INSERT INTO dbo.EventCourses (EventId, CourseId, IsEnabled) VALUES (@E, @C, @On)' @{ '@E' = $eid; '@C' = $cid; '@On' = $enabled })
                Add-Stat 'Event-course links'
            }
        }
    }

    # =======================================================================
    # AuditLog (entries are never changed once imported)
    # =======================================================================
    if ($auditDoc) {
        foreach ($a in $auditDoc.SelectNodes('/auditLog/entry')) {
            $id = $a.GetAttribute('id').Trim().ToLowerInvariant()
            $type = $a.GetAttribute('type').Trim()
            $ts = ConvertTo-UtcOrNull $a.GetAttribute('timestamp')
            if (-not $id -or $id.Length -gt 40) { Write-Warning "Audit entry skipped: bad id '$id'."; Add-Stat 'Audit entries skipped'; continue }
            if (-not $type -or $ts -is [DBNull]) { Write-Warning "Audit entry $id skipped: missing type or timestamp."; Add-Stat 'Audit entries skipped'; continue }

            $email = $a.GetAttribute('email').Trim().ToLowerInvariant()
            $p = @{
                '@Id' = $id; '@Ts' = $ts; '@Type' = $type
                '@Email' = (OrNull $email)
                '@First' = (OrNull $a.GetAttribute('firstName'))
                '@Role' = (OrNull $a.GetAttribute('role'))
                '@Uni' = (OrNull $a.GetAttribute('university'))
                '@Details' = (OrNull (Get-ChildText $a 'details'))
            }

            $result = Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.AuditLog WHERE LogId = @Id)
    SELECT 'already present';
ELSE
BEGIN
    INSERT INTO dbo.AuditLog (LogId, TimestampUtc, Type, Email, FirstName, Role, University, Details)
    VALUES (@Id, @Ts, @Type, @Email, @First, @Role, @Uni, @Details);
    SELECT 'inserted';
END
'@ $p
            Add-Stat "Audit entries $result"
        }
    }

    # =======================================================================
    # Finish
    # =======================================================================
    Write-Host ""
    foreach ($k in $stats.Keys) { Write-Host ("  {0,-45} {1}" -f $k, $stats[$k]) }
    Write-Host ""

    if ($DryRun) {
        $script:tx.Rollback()
        Write-Host "DRY RUN: nothing was saved." -ForegroundColor Yellow
    } else {
        $script:tx.Commit()
        Write-Host "Import committed." -ForegroundColor Green
    }
}
catch {
    $script:tx.Rollback()
    Write-Error "Import failed and was rolled back. Nothing was saved. $($_.Exception.Message)"
}
finally {
    $script:conn.Close()
}
