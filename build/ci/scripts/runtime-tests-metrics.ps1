<#
.SYNOPSIS
    Collects the metrics runtime tests record (RuntimeTestMetrics) and reports them on the pull request.

.DESCRIPTION
    Extract (runs in each runtime-test lane): reads the NUnit results file, where the runner writes each test's
    metrics as test-case properties (metric:<name>, budget:<name>, unit:<name>), and saves them as JSON under
    <OutputDirectory>/<Lane>/ for the shared 'runtime-tests-metrics' artifact.

    Report (runs once, after the lanes): merges the lanes' JSON into one markdown table, writes it to ReportFile,
    and on a pull request creates or updates a single comment carrying it (found again through a hidden marker),
    so pushes and stage retries update the comment instead of adding new ones.

    Report-only: a value over its budget is flagged, never fails the build. See specs/rendering-budget-tests/spec.md.

.EXAMPLE
    # Preview a report locally from a results file, without posting anything:
    ./runtime-tests-metrics.ps1 -Mode Extract -ResultsFile results.xml -Lane skia-windows -OutputDirectory out
    ./runtime-tests-metrics.ps1 -Mode Report -InputDirectory out -ReportFile report.md
#>
param(
    [Parameter(Mandatory)][ValidateSet('Extract', 'Report')][string]$Mode,

    # Extract
    [string]$ResultsFile,
    [string]$Lane,
    [string]$OutputDirectory,
    [int]$Attempt = 1,

    # Report
    [string]$InputDirectory,
    [string]$ReportFile,
    [string]$Repository,         # owner/name, e.g. unoplatform/uno
    [string]$PullRequestNumber,  # empty outside pull requests: nothing is posted
    [string]$CommitId,
    [string]$BuildUrl
)

$ErrorActionPreference = 'Stop'
$Marker = '<!-- runtime-tests-metrics -->'
$Invariant = [System.Globalization.CultureInfo]::InvariantCulture

function Format-Number([double]$value) {
    if ($value -eq [math]::Floor($value)) { return $value.ToString('0', $Invariant) }
    return $value.ToString('0.##', $Invariant)
}

function Invoke-Extract {
    if (-not (Test-Path $ResultsFile)) {
        Write-Host "No results file at '$ResultsFile' (the test run did not complete); nothing to extract."
        return
    }

    [xml]$results = Get-Content -Raw -Path $ResultsFile
    $metrics = foreach ($testCase in $results.SelectNodes('//test-case[properties/property]')) {
        $properties = @{}
        foreach ($property in $testCase.properties.property) { $properties[$property.name] = $property.value }

        foreach ($key in $properties.Keys | Where-Object { $_.StartsWith('metric:') } | Sort-Object) {
            $name = $key.Substring('metric:'.Length)
            [ordered]@{
                test   = $testCase.GetAttribute('fullname')
                result = $testCase.GetAttribute('result')
                name   = $name
                value  = [double]::Parse($properties[$key], $Invariant)
                budget = if ($properties.ContainsKey("budget:$name")) { [double]::Parse($properties["budget:$name"], $Invariant) } else { $null }
                unit   = $properties["unit:$name"]
            }
        }
    }

    $laneDirectory = Join-Path $OutputDirectory $Lane
    New-Item -ItemType Directory -Force -Path $laneDirectory | Out-Null
    # One file per job attempt: a retry re-runs only the failed tests, so the report merges attempts, latest last.
    $outputFile = Join-Path $laneDirectory "metrics-attempt$Attempt.json"
    ConvertTo-Json -InputObject @($metrics) -Depth 3 | Set-Content -Path $outputFile -Encoding utf8
    Write-Host "Extracted $(@($metrics).Count) metrics for lane '$Lane' to $outputFile"
    # Tells the publish step there is something to publish (it would fail on a missing folder otherwise).
    Write-Host '##vso[task.setvariable variable=RuntimeTestsMetricsExtracted]true'
}

function Get-LaneMetrics {
    $lanes = [ordered]@{}
    if (-not (Test-Path $InputDirectory)) { return $lanes }

    foreach ($laneDirectory in Get-ChildItem -Directory -Path $InputDirectory | Sort-Object Name) {
        $byKey = [ordered]@{}
        foreach ($file in Get-ChildItem -File -Path $laneDirectory.FullName -Filter 'metrics-attempt*.json' | Sort-Object { [int]($_.BaseName -replace '\D', '') }) {
            foreach ($metric in @(Get-Content -Raw -Path $file.FullName | ConvertFrom-Json)) {
                $byKey["$($metric.test)|$($metric.name)"] = $metric
            }
        }
        if ($byKey.Count -gt 0) { $lanes[$laneDirectory.Name] = $byKey }
    }
    return $lanes
}

function New-Report($lanes) {
    $laneNames = @($lanes.Keys)
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine($Marker)
    [void]$sb.AppendLine('### 📏 Rendering budgets')
    [void]$sb.AppendLine()

    if ($laneNames.Count -eq 0) {
        [void]$sb.AppendLine('No runtime-test metrics were collected for this build (the test lanes did not complete, or the tests did not run).')
    }
    else {
        # Rows keep the order the tests recorded them in, grouped by test.
        $rows = [ordered]@{}
        foreach ($lane in $laneNames) {
            foreach ($metric in $lanes[$lane].Values) {
                $key = "$($metric.test)|$($metric.name)"
                if (-not $rows.Contains($key)) { $rows[$key] = [ordered]@{ test = $metric.test; name = $metric.name; budget = $metric.budget; unit = $metric.unit } }
            }
        }

        $overBudget = [System.Collections.Generic.HashSet[string]]::new()
        $lines = foreach ($row in $rows.Values) {
            $budget = if ($null -ne $row.budget) { '≤ ' + (Format-Number $row.budget) } else { '–' }
            $cells = foreach ($lane in $laneNames) {
                $metric = $lanes[$lane]["$($row.test)|$($row.name)"]
                if ($null -eq $metric) { '–'; continue }
                $text = (Format-Number $metric.value) + $metric.unit
                if ($null -ne $metric.budget -and $metric.value -gt $metric.budget) { [void]$overBudget.Add($row.name); "**$text** ⚠️" } else { $text }
            }
            "| ``$($row.name)`` | $budget | $($cells -join ' | ') |"
        }

        if ($overBudget.Count -eq 0) {
            [void]$sb.AppendLine("✅ All $($rows.Count) metrics are within budget on every lane.")
        }
        else {
            [void]$sb.AppendLine("⚠️ $($overBudget.Count) of $($rows.Count) metrics are over budget on at least one lane. Report only for now: this does not fail the build.")
        }
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("| Metric | Budget | $($laneNames -join ' | ') |")
        [void]$sb.AppendLine("|---|---|$(($laneNames | ForEach-Object { '---:' }) -join '|')|")
        $lines | ForEach-Object { [void]$sb.AppendLine($_) }
    }

    [void]$sb.AppendLine()
    [void]$sb.AppendLine('<sub>Counts, not timings: frames, damaged area, path operations, measures and objects left alive, recorded by the `Given_RenderingBudget` runtime tests.')
    $footer = @()
    if ($BuildUrl) { $footer += "[Build]($($BuildUrl -replace ' ', '%20'))" } # The project name has a space.
    # Outside pull requests the pipeline passes the unexpanded macro; only a real hash is shown.
    if ($CommitId -match '^[0-9a-f]{7,40}$') { $footer += "commit $($CommitId.Substring(0, [math]::Min(10, $CommitId.Length)))" }
    $footer += "updated $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm', $Invariant)) UTC"
    [void]$sb.AppendLine(($footer -join ' · ') + '</sub>')
    return $sb.ToString()
}

function Publish-Comment([string]$body) {
    $token = $env:GITHUB_TOKEN
    if ([string]::IsNullOrWhiteSpace($PullRequestNumber) -or -not ($PullRequestNumber -match '^\d+$')) {
        Write-Host 'Not a pull request build; the report is only published as an artifact.'
        return
    }
    if ([string]::IsNullOrWhiteSpace($token) -or $token.StartsWith('$(')) {
        # Plain output, not a pipeline warning: a fork pull request has no secrets, and that is not a problem to flag.
        Write-Host 'No GitHub token available (fork pull request?); the report was not posted.'
        return
    }

    $headers = @{ Authorization = "token $token"; Accept = 'application/vnd.github+json'; 'User-Agent' = 'uno-runtime-tests-metrics' }
    $api = "https://api.github.com/repos/$Repository/issues"

    $existing = $null
    for ($page = 1; $page -le 20 -and -not $existing; $page++) {
        $comments = @(Invoke-RestMethod -Headers $headers -Uri "$api/$PullRequestNumber/comments?per_page=100&page=$page")
        $existing = $comments | Where-Object { $_.body -and $_.body.StartsWith($Marker) } | Select-Object -First 1
        if ($comments.Count -lt 100) { break }
    }

    $payload = @{ body = $body } | ConvertTo-Json
    if ($existing) {
        Invoke-RestMethod -Headers $headers -Method Patch -Uri "$api/comments/$($existing.id)" -Body $payload -ContentType 'application/json' | Out-Null
        Write-Host "Updated comment $($existing.html_url)"
    }
    else {
        $created = Invoke-RestMethod -Headers $headers -Method Post -Uri "$api/$PullRequestNumber/comments" -Body $payload -ContentType 'application/json'
        Write-Host "Created comment $($created.html_url)"
    }
}

switch ($Mode) {
    'Extract' { Invoke-Extract }
    'Report' {
        $report = New-Report (Get-LaneMetrics)
        if ($ReportFile) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent ([System.IO.Path]::GetFullPath($ReportFile))) | Out-Null
            Set-Content -Path $ReportFile -Value $report -Encoding utf8
        }
        Write-Host $report
        Publish-Comment $report
    }
}
