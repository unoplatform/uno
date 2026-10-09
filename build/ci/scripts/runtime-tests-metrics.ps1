<#
.SYNOPSIS
    Collects the metrics runtime tests record (RuntimeTestMetrics) and reports them on the pull request.

.DESCRIPTION
    Extract (runs in each runtime-test lane): reads the NUnit results file, where the runner writes each test's
    metrics as test-case properties (metric:<name>, budget:<name>, unit:<name>), and saves them as JSON under
    <OutputDirectory>/<Lane>/ for the shared 'runtime-tests-metrics' artifact.

    Report (runs once, after the lanes): writes one markdown report to ReportFile and, on a pull request, creates
    or updates a single comment carrying it (found again through a hidden marker and its author), so pushes and
    stage retries update the comment instead of adding new ones.

    The report compares this build with the latest published versions: the latest dev and the latest stable
    Uno.WinUI on nuget.org. Each version is mapped to the CI build that produced it (its build number is the
    version) and that build's own 'runtime-tests-metrics' artifact gives its numbers. The comparison is therefore
    with what users actually have, and follows the stable line as it moves from one release branch to the next.

    Report-only: a value over its budget is flagged, never fails the build. See specs/rendering-budget-tests/spec.md.

.EXAMPLE
    # Preview a report locally from a results file, without posting anything:
    ./runtime-tests-metrics.ps1 -Mode Extract -ResultsFile results.xml -Lane skia-windows -OutputDirectory out
    ./runtime-tests-metrics.ps1 -Mode Report -InputDirectory out -ReportFile report.md -BuildNumber 7.0-dev.1 `
        -CollectionUri https://dev.azure.com/uno-platform/ -ProjectId 1dd81cbd-cb35-41de-a570-b0df3571a196 -DefinitionId 5
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
    [string]$BuildUrl,

    # Report: comparison with the published versions
    [string]$CollectionUri,      # e.g. https://dev.azure.com/uno-platform/
    [string]$ProjectId,
    [string]$DefinitionId,
    [string]$BuildNumber,        # this build's, e.g. 7.0-dev.1662+db89a9843c: selects the dev line to compare with
    [string]$ReferenceLane = 'skia-windows',
    [string]$DevBuildNumber,     # overrides for a manual comparison; by default, the builds of the latest
    [string]$StableBuildNumber   # dev and stable Uno.WinUI versions on nuget.org
)

$ErrorActionPreference = 'Stop'
$Marker = '<!-- runtime-tests-metrics -->'
$Invariant = [System.Globalization.CultureInfo]::InvariantCulture

function Format-Number([double]$value) {
    if ($value -eq [math]::Floor($value)) { return $value.ToString('0', $Invariant) }
    return $value.ToString('0.##', $Invariant)
}

function Format-Metric($metric) {
    if ($null -eq $metric) { return '–' }
    $text = (Format-Number $metric.value) + $metric.unit
    if ($null -ne $metric.budget -and $metric.value -gt $metric.budget) { return "**$text** ⚠️" }
    return $text
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

function Get-LaneMetrics([string]$directory) {
    $lanes = [ordered]@{}
    if (-not $directory -or -not (Test-Path $directory)) { return $lanes }

    foreach ($laneDirectory in Get-ChildItem -Directory -Path $directory | Sort-Object Name) {
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

# The published versions to compare with: the latest dev of this build's line (7.0 for master, 6.8 for
# servicing/6.8...) and the latest stable, as nuget.org lists them, each with the CI build number that produced it.
function Get-PublishedBaselines {
    $versions = @()
    if (-not $DevBuildNumber -or -not $StableBuildNumber) {
        try { $versions = @((Invoke-RestMethod -Uri 'https://api.nuget.org/v3-flatcontainer/uno.winui/index.json').versions) }
        catch { Write-Host "Could not read the published versions from nuget.org: $($_.Exception.Message)" }
    }

    $baselines = @()
    if ($DevBuildNumber) {
        $baselines += [ordered]@{ label = 'Latest dev'; version = $DevBuildNumber; buildNumber = $DevBuildNumber }
    }
    elseif ($BuildNumber -match '^(\d+)\.(\d+)') {
        $line = "$($Matches[1]).$($Matches[2])"
        $dev = $versions | Where-Object { $_ -match "^$([regex]::Escape($line))\.0-dev\.\d+$" } |
            Sort-Object { [int]($_ -replace '^.*-dev\.', '') } | Select-Object -Last 1
        if ($dev) {
            # 7.0.0-dev.1656 on nuget.org is CI build number 7.0-dev.1656.
            $baselines += [ordered]@{ label = 'Latest dev'; version = $dev; buildNumber = ($dev -replace '^(\d+\.\d+)\.0-dev\.', '$1-dev.') }
        }
    }

    if ($StableBuildNumber) {
        $baselines += [ordered]@{ label = 'Latest stable'; version = $StableBuildNumber; buildNumber = $StableBuildNumber }
    }
    else {
        $stable = $versions | Where-Object { $_ -match '^\d+\.\d+\.\d+$' } | Sort-Object { [version]$_ } | Select-Object -Last 1
        if ($stable) { $baselines += [ordered]@{ label = 'Latest stable'; version = $stable; buildNumber = $stable } }
    }
    return $baselines
}

function Get-BaselineMetrics($baseline) {
    $result = [ordered]@{ label = $baseline.label; version = $baseline.version; buildId = $null; buildUrl = $null; lanes = $null; note = $null }
    if (-not ($CollectionUri -and $ProjectId -and $DefinitionId)) {
        $result.note = 'no CI connection to look its build up'
        return $result
    }

    # The project is public, so the token is only a courtesy; it is absent when the script runs locally.
    $headers = @{}
    if ($env:SYSTEM_ACCESSTOKEN -and -not $env:SYSTEM_ACCESSTOKEN.StartsWith('$(')) { $headers.Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN" }
    $api = "$($CollectionUri.TrimEnd('/'))/$ProjectId/_apis/build/builds"

    try {
        $query = "definitions=$DefinitionId&buildNumber=$([uri]::EscapeDataString($baseline.buildNumber))&statusFilter=completed&queryOrder=finishTimeDescending&api-version=7.1"
        $builds = @((Invoke-RestMethod -Headers $headers -Uri "$api`?$query").value)
        if ($builds.Count -eq 0) {
            $result.note = "no CI build numbered $($baseline.buildNumber) was found"
            return $result
        }

        foreach ($build in $builds) {
            $result.buildId = $build.id
            $result.buildUrl = $build._links.web.href
            try { $artifact = Invoke-RestMethod -Headers $headers -Uri "$api/$($build.id)/artifacts?artifactName=runtime-tests-metrics&api-version=7.1" }
            catch { continue } # Not found: the build ran before the tests existed.

            $directory = Join-Path ([System.IO.Path]::GetTempPath()) "runtime-tests-metrics-$($build.id)"
            $zip = "$directory.zip"
            Invoke-WebRequest -Headers $headers -Uri $artifact.resource.downloadUrl -OutFile $zip
            Expand-Archive -Path $zip -DestinationPath $directory -Force
            $result.lanes = Get-LaneMetrics (Join-Path $directory 'runtime-tests-metrics')
            if ($result.lanes.Count -gt 0) { return $result }
            $result.lanes = $null
        }
        $result.note = 'its build ran before these tests existed'
    }
    catch {
        $result.note = "its build could not be read ($($_.Exception.Message))"
    }
    return $result
}

function New-Report($lanes, $baselines) {
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
                if (-not $rows.Contains($key)) { $rows[$key] = [ordered]@{ key = $key; name = $metric.name; budget = $metric.budget } }
            }
        }

        # This PR next to the latest published versions, on one lane so the columns compare like with like.
        $reference = if ($lanes.Contains($ReferenceLane)) { $ReferenceLane } else { $laneNames[0] }
        $dev = $baselines | Where-Object { $_.label -eq 'Latest dev' } | Select-Object -First 1
        $isOver = { param($m) $null -ne $m -and $null -ne $m.budget -and $m.value -gt $m.budget }

        $tableLines = [System.Collections.Generic.List[string]]::new()
        $lower = 0
        $higher = 0
        foreach ($row in $rows.Values) {
            $budget = if ($null -ne $row.budget) { '≤ ' + (Format-Number $row.budget) } else { '–' }
            $current = $lanes[$reference][$row.key]
            $cell = Format-Metric $current

            # Every budgeted metric is a count where lower is better; flag a clear move away from the latest dev, past
            # run-to-run noise (the damaged area alone varies by about 13% between runs of the same build). Frame rates
            # follow the agent's display rate, so for them only crossing the budget is a change.
            $devMetric = if ($dev -and $dev.lanes -and $dev.lanes.Contains($reference)) { $dev.lanes[$reference][$row.key] } else { $null }
            $arrow = $null
            if ($null -ne $current -and $null -ne $devMetric -and $null -ne $row.budget) {
                if ($row.name.EndsWith('frames-per-second')) {
                    $overNow = & $isOver $current
                    if ($overNow -ne (& $isOver $devMetric)) { $arrow = if ($overNow) { '▲' } else { '▼' } }
                }
                else {
                    $difference = $current.value - $devMetric.value
                    if ([math]::Abs($difference) -gt [math]::Max(0.05, 0.2 * [math]::Abs($devMetric.value))) {
                        $arrow = if ($difference -gt 0) { '▲' } else { '▼' }
                    }
                }
            }
            if ($arrow) {
                $cell += " $arrow"
                if ($arrow -eq '▲') { $higher++ } else { $lower++ }
            }

            $baselineCells = foreach ($b in $baselines) {
                if ($b.lanes -and $b.lanes.Contains($reference)) { Format-Metric $b.lanes[$reference][$row.key] } else { '–' }
            }
            $tableLines.Add("| ``$($row.name)`` | $budget | $cell | $($baselineCells -join ' | ') |")
        }

        # One summary line per column, so "within budget" is never read as being about another version.
        $prOver = @($rows.Values | Where-Object { $key = $_.key; @($laneNames | Where-Object { & $isOver $lanes[$_][$key] }).Count -gt 0 }).Count
        $prLine = if ($prOver -eq 0) { "✅ all $($rows.Count) metrics within budget on every lane" } else { "⚠️ $prOver of $($rows.Count) metrics over budget on at least one lane" }
        if ($dev -and $dev.lanes) {
            $prLine += if ($lower + $higher -eq 0) { '; no change from the latest dev' } else { "; against the latest dev: $lower lower ▼, $higher higher ▲" }
        }
        [void]$sb.AppendLine("- **This PR**: $prLine.")
        foreach ($b in $baselines) {
            $build = if ($b.buildUrl) { "[$($b.version)]($($b.buildUrl -replace ' ', '%20'))" } else { $b.version }
            if ($b.lanes -and $b.lanes.Contains($reference)) {
                # Over the rows shown: a version's own extra rows (which objects it leaked) are not in this table.
                $over = @($rows.Values | Where-Object { & $isOver $b.lanes[$reference][$_.key] }).Count
                $state = if ($over -eq 0) { '✅ within budget' } else { "⚠️ $over over budget" }
                [void]$sb.AppendLine("- **$($b.label)** $($build): $state.")
            }
            else {
                [void]$sb.AppendLine("- **$($b.label)** $($build): no numbers, $($b.note).")
            }
        }
        [void]$sb.AppendLine()
        [void]$sb.AppendLine('Report only for now: nothing here fails the build.')
        [void]$sb.AppendLine()

        $columns = foreach ($b in $baselines) {
            $version = if ($b.buildUrl) { "[$($b.version)]($($b.buildUrl -replace ' ', '%20'))" } else { $b.version }
            "$($b.label)<br>$version"
        }
        [void]$sb.AppendLine("**Compared with the latest published versions** (lane ``$reference``)")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("| Metric | Budget | This PR | $($columns -join ' | ') |")
        [void]$sb.AppendLine("|---|---|---:|$(($baselines | ForEach-Object { '---:' }) -join '|')|")
        $tableLines | ForEach-Object { [void]$sb.AppendLine($_) }
        [void]$sb.AppendLine()
        [void]$sb.AppendLine('▲ / ▼: higher / lower than the latest dev by more than 20%; lower is better for every budgeted count. Frame rates follow the agent''s display rate, so they are only compared when they cross their budget.')
        [void]$sb.AppendLine()

        # Every lane of this PR, for differences between platforms and renderers.
        [void]$sb.AppendLine('<details><summary>This PR on every lane</summary>')
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("| Metric | Budget | $($laneNames -join ' | ') |")
        [void]$sb.AppendLine("|---|---|$(($laneNames | ForEach-Object { '---:' }) -join '|')|")
        foreach ($row in $rows.Values) {
            $budget = if ($null -ne $row.budget) { '≤ ' + (Format-Number $row.budget) } else { '–' }
            $cells = foreach ($lane in $laneNames) { Format-Metric $lanes[$lane][$row.key] }
            [void]$sb.AppendLine("| ``$($row.name)`` | $budget | $($cells -join ' | ') |")
        }
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("Frame rates follow each lane's display rate (the framebuffer lane has no vsync), so compare them within a lane.")
        [void]$sb.AppendLine('</details>')
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
        Write-Host 'No GitHub token available (fork pull request, or the pipeline secret is not set); the report was not posted.'
        return
    }

    $headers = @{ Authorization = "Bearer $token"; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28'; 'User-Agent' = 'uno-runtime-tests-metrics' }
    $api = "https://api.github.com/repos/$Repository/issues"

    # Informational only: a failed post is logged and the build goes on (the report is still in the artifact).
    try {
        # Only a comment this account wrote can be edited: editing someone else's needs admin rights on the
        # repository, so a marker comment left by another account is ignored and a new one is created.
        $login = (Invoke-RestMethod -Headers $headers -Uri 'https://api.github.com/user').login

        $existing = $null
        for ($page = 1; $page -le 20 -and -not $existing; $page++) {
            $comments = @(Invoke-RestMethod -Headers $headers -Uri "$api/$PullRequestNumber/comments?per_page=100&page=$page")
            $existing = $comments | Where-Object { $_.user.login -eq $login -and $_.body -and $_.body.StartsWith($Marker) } | Select-Object -First 1
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
    catch {
        Write-Host "The report was not posted: $($_.Exception.Message) $($_.ErrorDetails.Message)"
    }
}

switch ($Mode) {
    'Extract' { Invoke-Extract }
    'Report' {
        $lanes = Get-LaneMetrics $InputDirectory
        $baselines = @(Get-PublishedBaselines | ForEach-Object { Get-BaselineMetrics $_ })
        $report = New-Report $lanes $baselines
        if ($ReportFile) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent ([System.IO.Path]::GetFullPath($ReportFile))) | Out-Null
            Set-Content -Path $ReportFile -Value $report -Encoding utf8
        }
        Write-Host $report
        Publish-Comment $report
    }
}
