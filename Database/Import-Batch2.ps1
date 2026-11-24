<#
.SYNOPSIS
    One-time import of the quiz XML files into the database:
      quiz_rules.xml     -> QuizRulesets, QuizDomains
      quiz_questions.xml -> QuizQuestions, QuizOptions, QuizOptionWeights
      quiz_progress.xml  -> QuizProgress, QuizProgressAnswers
      quiz_results.xml   -> QuizResults, QuizResultDomainScores, QuizResultItems

.DESCRIPTION
    - Run AFTER Import-UsersXml.ps1 and Import-Batch1.ps1 (needs Users and Microcourses).
    - Read-only on the XML files. Safe to re-run: rows are updated and their
      child lists replaced.
    - One transaction: if anything fails, nothing is saved.
    - Skips (and reports) progress/results for users that aren't in the Users
      table, answers to unknown questions/options, and unknown domains.

.EXAMPLE
    & "C:\path\Import-Batch2.ps1" -AppDataPath "C:\path\App_Data" `
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
    if (-not (Test-Path -LiteralPath $path)) { Write-Warning "$relativePath not found; skipping it."; return $null }
    $doc = New-Object System.Xml.XmlDocument
    $doc.Load((Resolve-Path -LiteralPath $path).Path)
    return ,$doc   # leading comma stops PowerShell from unrolling the XmlDocument
}

function Get-ChildText([System.Xml.XmlNode] $node, [string] $name) {
    $el = $node.SelectSingleNode($name)
    if ($null -eq $el) { return '' }
    return (($el.InnerText -replace '\s+', ' ').Trim())
}

# Text directly inside an element, ignoring child elements like <Map/>.
function Get-OwnText([System.Xml.XmlNode] $node) {
    $parts = foreach ($n in $node.ChildNodes) {
        if ($n.NodeType -eq 'Text' -or $n.NodeType -eq 'CDATA') { $n.Value }
    }
    return ((($parts -join ' ') -replace '\s+', ' ').Trim())
}

function ConvertTo-UtcOrNull([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return [DBNull]::Value }
    $styles = [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal
    return [DateTime]::Parse($value.Trim(), [Globalization.CultureInfo]::InvariantCulture, $styles)
}

function ConvertTo-DecimalOrNull([string] $value) {
    $d = [decimal]0
    if ([decimal]::TryParse($value.Trim(), [Globalization.NumberStyles]::Number,
                            [Globalization.CultureInfo]::InvariantCulture, [ref] $d)) { return $d }
    return $null
}

function ConvertTo-IntOrDefault([string] $value, [int] $default) {
    $n = 0
    if ([int]::TryParse($value.Trim(), [ref] $n)) { return $n }
    return $default
}

function OrNull([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return [DBNull]::Value }
    return $value.Trim()
}

function Invoke-Sql([string] $sql, [hashtable] $params = @{}) {
    $cmd = $script:conn.CreateCommand()
    $cmd.Transaction = $script:tx
    $cmd.CommandText = $sql
    foreach ($key in $params.Keys) {
        $value = $params[$key]
        if ($null -eq $value) { $value = [DBNull]::Value }
        $prm = $cmd.Parameters.AddWithValue($key, $value)
        if ($value -is [DateTime]) { $prm.SqlDbType = [System.Data.SqlDbType]::DateTime2 }
    }
    $result = $cmd.ExecuteScalar()
    $cmd.Dispose()
    return $result
}

function Get-Column([string] $sql) {
    $set = @{}
    $cmd = $script:conn.CreateCommand()
    $cmd.Transaction = $script:tx
    $cmd.CommandText = $sql
    $reader = $cmd.ExecuteReader()
    try { while ($reader.Read()) { $set[[string]$reader.GetValue(0)] = $true } }
    finally { $reader.Close(); $cmd.Dispose() }
    return $set
}

$stats = [ordered]@{}
function Add-Stat([string] $name) { if ($stats.Contains($name)) { $stats[$name]++ } else { $stats[$name] = 1 } }

# Make sure a ruleset version row exists (old results may use versions that
# no longer appear in quiz_rules.xml).
$knownRulesets = @{}
function Confirm-Ruleset([string] $version) {
    if ($knownRulesets.ContainsKey($version)) { return }
    [void](Invoke-Sql @'
IF NOT EXISTS (SELECT 1 FROM dbo.QuizRulesets WHERE Version = @V)
    INSERT INTO dbo.QuizRulesets (Version, CreatedAtUtc) VALUES (@V, NULL);
'@ @{ '@V' = $version })
    $knownRulesets[$version] = $true
}

# ---------------------------------------------------------------------------
# Load XML and connect
# ---------------------------------------------------------------------------
$rulesDoc     = Get-XmlFile 'quiz_rules.xml'
$questionsDoc = Get-XmlFile 'quiz_questions.xml'
$progressDoc  = Get-XmlFile 'quiz_progress.xml'
$resultsDoc   = Get-XmlFile 'quiz_results.xml'

$script:conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$script:conn.Open()
$script:tx = $script:conn.BeginTransaction()

try {
    $userIds = Get-Column 'SELECT UserId FROM dbo.Users'

    # =======================================================================
    # Rulesets and domains
    # =======================================================================
    $domainKeys = @{}      # key -> $true          (hashtables ignore case)
    $domainByTitle = @{}   # title -> key

    if ($rulesDoc) {
        foreach ($rs in $rulesDoc.SelectNodes('/QuizRules/Ruleset')) {
            $version = $rs.GetAttribute('version').Trim()
            if (-not $version) { Write-Warning 'A <Ruleset> has no version; skipped.'; continue }

            [void](Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.QuizRulesets WHERE Version = @V)
    UPDATE dbo.QuizRulesets SET CreatedAtUtc = @C WHERE Version = @V;
ELSE
    INSERT INTO dbo.QuizRulesets (Version, CreatedAtUtc) VALUES (@V, @C);
'@ @{ '@V' = $version; '@C' = (ConvertTo-UtcOrNull $rs.GetAttribute('createdUtc')) })
            $knownRulesets[$version] = $true
            Add-Stat 'Rulesets'

            foreach ($d in $rs.SelectNodes('Domain')) {
                $key = $d.GetAttribute('id').Trim()
                $title = ($d.GetAttribute('title') -replace '\s+', ' ').Trim()
                if (-not $key -or -not $title) { Write-Warning 'A <Domain> is missing id or title; skipped.'; continue }

                [void](Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.QuizDomains WHERE DomainKey = @K)
    UPDATE dbo.QuizDomains
       SET Title = @T, QuickWin = @Q, Factor = @F, RulesetVersion = @V
     WHERE DomainKey = @K;
ELSE
    INSERT INTO dbo.QuizDomains (DomainKey, Title, QuickWin, Factor, RulesetVersion)
    VALUES (@K, @T, @Q, @F, @V);
'@ @{ '@K' = $key; '@T' = $title; '@Q' = (OrNull $d.GetAttribute('quickWin'));
      '@F' = (OrNull $d.GetAttribute('factor')); '@V' = $version })

                $domainKeys[$key] = $true
                $domainByTitle[$title] = $key
                Add-Stat 'Domains'
            }
        }

        # Link each domain to the microcourse with the same title (case-insensitive).
        $linked = Invoke-Sql @'
UPDATE d
   SET CourseId = (SELECT TOP 1 m.CourseId FROM dbo.Microcourses m
                   WHERE LTRIM(RTRIM(m.Title)) = d.Title ORDER BY m.CreatedAtUtc)
  FROM dbo.QuizDomains d;
SELECT COUNT(*) FROM dbo.QuizDomains WHERE CourseId IS NOT NULL;
'@
        $stats['Domains linked to a microcourse'] = $linked
    }

    # =======================================================================
    # Questions, options, weights
    # =======================================================================
    $optionKeys = @{}   # "q1|A" -> $true

    if ($questionsDoc) {
        $order = 0
        foreach ($q in $questionsDoc.SelectNodes('/QuizQuestions/Question')) {
            $order++
            $qid = $q.GetAttribute('id').Trim()
            $text = Get-ChildText $q 'Text'
            if (-not $qid -or -not $text) { Write-Warning "Question #$order skipped: missing id or text."; Add-Stat 'Questions skipped'; continue }

            [void](Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuestionId = @Q)
    UPDATE dbo.QuizQuestions SET QuestionText = @T, SortOrder = @S WHERE QuestionId = @Q;
ELSE
    INSERT INTO dbo.QuizQuestions (QuestionId, QuestionText, SortOrder) VALUES (@Q, @T, @S);
'@ @{ '@Q' = $qid; '@T' = $text; '@S' = $order })
            Add-Stat 'Questions'

            # Replace this question's options and weights. Options that existing
            # answers point to can't be deleted, so update/insert instead of delete-all.
            [void](Invoke-Sql 'DELETE FROM dbo.QuizOptionWeights WHERE QuestionId = @Q' @{ '@Q' = $qid })

            foreach ($opt in $q.ChildNodes) {
                if ($opt.NodeType -ne 'Element' -or $opt.Name -cnotmatch '^[A-Z]$') { continue }
                $letter = $opt.Name
                $optText = Get-OwnText $opt
                if (-not $optText) { Write-Warning "Question ${qid} option ${letter}: no text; skipped."; continue }

                [void](Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.QuizOptions WHERE QuestionId = @Q AND OptionKey = @O)
    UPDATE dbo.QuizOptions SET OptionText = @T WHERE QuestionId = @Q AND OptionKey = @O;
ELSE
    INSERT INTO dbo.QuizOptions (QuestionId, OptionKey, OptionText) VALUES (@Q, @O, @T);
'@ @{ '@Q' = $qid; '@O' = $letter; '@T' = $optText })
                $optionKeys["$qid|$letter"] = $true
                Add-Stat 'Options'

                $seen = @{}
                foreach ($map in $opt.SelectNodes('Map')) {
                    $module = $map.GetAttribute('module').Trim()
                    $weight = 0
                    if (-not $domainKeys.ContainsKey($module)) {
                        Write-Warning "Question ${qid} option ${letter}: unknown domain '$module'; weight skipped."
                        Add-Stat 'Weights skipped'; continue
                    }
                    if (-not [int]::TryParse($map.GetAttribute('weight').Trim(), [ref] $weight)) {
                        Write-Warning "Question ${qid} option ${letter}: weight '$($map.GetAttribute('weight'))' is not a whole number; skipped."
                        Add-Stat 'Weights skipped'; continue
                    }
                    if ($seen.ContainsKey($module)) { continue }
                    $seen[$module] = $true
                    [void](Invoke-Sql 'INSERT INTO dbo.QuizOptionWeights (QuestionId, OptionKey, DomainKey, Weight) VALUES (@Q, @O, @D, @W)' `
                        @{ '@Q' = $qid; '@O' = $letter; '@D' = $module; '@W' = $weight })
                    Add-Stat 'Option weights'
                }
            }
        }
    }

    # =======================================================================
    # Progress (one row per user)
    # =======================================================================
    if ($progressDoc) {
        foreach ($u in $progressDoc.SelectNodes('/Progress/User')) {
            $uid = $u.GetAttribute('name').Trim().ToLowerInvariant()
            if (-not $userIds.ContainsKey($uid)) {
                Write-Warning "Quiz progress for user $uid skipped: user not in the Users table."
                Add-Stat 'Progress skipped (unknown user)'; continue
            }
            $version = $u.GetAttribute('rules').Trim()
            if (-not $version) { $version = '2.0' }
            Confirm-Ruleset $version

            $answers = $u.SelectNodes('Answers/Answer')
            $total = ConvertTo-IntOrDefault $u.GetAttribute('total') $answers.Count
            $last  = ConvertTo-IntOrDefault $u.GetAttribute('lastIndex') 0

            [void](Invoke-Sql @'
IF EXISTS (SELECT 1 FROM dbo.QuizProgress WHERE UserId = @U)
    UPDATE dbo.QuizProgress
       SET RulesetVersion = @V, LastIndex = @L, TotalQuestions = @T, IsCompleted = @C
     WHERE UserId = @U;
ELSE
    INSERT INTO dbo.QuizProgress (UserId, RulesetVersion, LastIndex, TotalQuestions, IsCompleted)
    VALUES (@U, @V, @L, @T, @C);
DELETE FROM dbo.QuizProgressAnswers WHERE UserId = @U;
'@ @{ '@U' = $uid; '@V' = $version; '@L' = $last; '@T' = $total;
      '@C' = ($u.GetAttribute('completed').Trim() -ieq 'true') })
            Add-Stat 'Progress records'

            $seen = @{}
            foreach ($a in $answers) {
                $qid = $a.GetAttribute('qid').Trim()
                $choice = $a.GetAttribute('choice').Trim().ToUpperInvariant()
                if ($seen.ContainsKey($qid)) { continue }
                if (-not $optionKeys.ContainsKey("$qid|$choice")) {
                    Write-Warning "User ${uid}: answer '$choice' to '$qid' isn't a known option; skipped."
                    Add-Stat 'Progress answers skipped'; continue
                }
                $seen[$qid] = $true
                [void](Invoke-Sql 'INSERT INTO dbo.QuizProgressAnswers (UserId, QuestionId, Choice) VALUES (@U, @Q, @C)' `
                    @{ '@U' = $uid; '@Q' = $qid; '@C' = $choice })
                Add-Stat 'Progress answers'
            }
        }
    }

    # =======================================================================
    # Results (several per user allowed)
    # =======================================================================
    if ($resultsDoc) {
        foreach ($r in $resultsDoc.SelectNodes('/Results/UserResult')) {
            $uid = $r.GetAttribute('name').Trim().ToLowerInvariant()
            if (-not $userIds.ContainsKey($uid)) {
                Write-Warning "Quiz result for user $uid skipped: user not in the Users table."
                Add-Stat 'Results skipped (unknown user)'; continue
            }
            $completed = ConvertTo-UtcOrNull $r.GetAttribute('completedUtc')
            $overall = ConvertTo-DecimalOrNull $r.GetAttribute('overall')
            if ($completed -is [DBNull] -or $null -eq $overall) {
                Write-Warning "Quiz result for user $uid skipped: missing completion time or overall score."
                Add-Stat 'Results skipped (bad data)'; continue
            }
            $version = $r.GetAttribute('rules').Trim()
            if (-not $version) { $version = '2.0' }
            Confirm-Ruleset $version

            $resultId = Invoke-Sql @'
DECLARE @Rid INT = (SELECT ResultId FROM dbo.QuizResults WHERE UserId = @U AND CompletedAtUtc = @C);
IF @Rid IS NULL
BEGIN
    INSERT INTO dbo.QuizResults (UserId, RulesetVersion, CompletedAtUtc, OverallScore, ShareWithHelper)
    VALUES (@U, @V, @C, @O, @S);
    SET @Rid = CAST(SCOPE_IDENTITY() AS INT);
END
ELSE
BEGIN
    UPDATE dbo.QuizResults SET RulesetVersion = @V, OverallScore = @O, ShareWithHelper = @S WHERE ResultId = @Rid;
    DELETE FROM dbo.QuizResultDomainScores WHERE ResultId = @Rid;
    DELETE FROM dbo.QuizResultItems WHERE ResultId = @Rid;
END
SELECT @Rid;
'@ @{ '@U' = $uid; '@V' = $version; '@C' = $completed; '@O' = $overall;
      '@S' = ($r.GetAttribute('shareWithHelper').Trim() -ieq 'true') }
            Add-Stat 'Results'

            # Domain scores: the XML stores titles, so map them back to keys.
            $seen = @{}
            foreach ($d in $r.SelectNodes('Domains/Domain')) {
                $title = ($d.GetAttribute('title') -replace '\s+', ' ').Trim()
                $score = ConvertTo-DecimalOrNull $d.GetAttribute('score')
                if (-not $domainByTitle.ContainsKey($title) -or $null -eq $score) {
                    Write-Warning "Result for user ${uid}: domain '$title' unknown or score missing; skipped."
                    Add-Stat 'Domain scores skipped'; continue
                }
                $key = $domainByTitle[$title]
                if ($seen.ContainsKey($key)) { continue }
                $seen[$key] = $true
                [void](Invoke-Sql 'INSERT INTO dbo.QuizResultDomainScores (ResultId, DomainKey, Score) VALUES (@R, @D, @S)' `
                    @{ '@R' = $resultId; '@D' = $key; '@S' = $score })
                Add-Stat 'Domain scores'
            }

            # Top factors and quick wins, kept in order
            foreach ($pair in @(@('TopFactors/Factor', 'Factor'), @('QuickWins/Win', 'QuickWin'))) {
                $i = 0
                foreach ($item in $r.SelectNodes($pair[0])) {
                    $txt = (($item.InnerText -replace '\s+', ' ').Trim())
                    if (-not $txt) { continue }
                    if ($txt.Length -gt 500) { $txt = $txt.Substring(0, 500) }
                    $i++
                    [void](Invoke-Sql 'INSERT INTO dbo.QuizResultItems (ResultId, ItemType, SortOrder, ItemText) VALUES (@R, @T, @S, @X)' `
                        @{ '@R' = $resultId; '@T' = $pair[1]; '@S' = $i; '@X' = $txt })
                    Add-Stat "Result items ($($pair[1]))"
                }
            }
        }
    }

    # =======================================================================
    # Finish
    # =======================================================================
    Write-Host ""
    foreach ($k in $stats.Keys) { Write-Host ("  {0,-40} {1}" -f $k, $stats[$k]) }
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
