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
                description = $properties["description:$name"]
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
# servicing/6.8...) and the latest stable, as nuget.org lists them. Each baseline lists candidate versions, newest
# first, with the CI build number that produced each. The stable one is only ever the last public release; the dev one
# falls back to the newest dev version that has numbers.
function Get-PublishedBaselines {
    $versions = @()
    if (-not $DevBuildNumber -or -not $StableBuildNumber) {
        try { $versions = @((Invoke-RestMethod -Uri 'https://api.nuget.org/v3-flatcontainer/uno.winui/index.json').versions) }
        catch { Write-Host "Could not read the published versions from nuget.org: $($_.Exception.Message)" }
    }

    $baselines = @()
    if ($DevBuildNumber) {
        $baselines += [ordered]@{ label = 'Latest dev'; short = 'dev'; candidates = @([ordered]@{ version = $DevBuildNumber; buildNumber = $DevBuildNumber }) }
    }
    elseif ($BuildNumber -match '^(\d+)\.(\d+)') {
        $line = "$($Matches[1]).$($Matches[2])"
        $devs = @($versions | Where-Object { $_ -match "^$([regex]::Escape($line))\.0-dev\.\d+$" } |
                Sort-Object { [int]($_ -replace '^.*-dev\.', '') } -Descending | Select-Object -First 20)
        if ($devs.Count -gt 0) {
            # 7.0.0-dev.1656 on nuget.org is CI build number 7.0-dev.1656.
            $candidates = @($devs | ForEach-Object { [ordered]@{ version = $_; buildNumber = ($_ -replace '^(\d+\.\d+)\.0-dev\.', '$1-dev.') } })
            $baselines += [ordered]@{ label = 'Latest dev'; short = 'dev'; candidates = $candidates }
        }
    }

    if ($StableBuildNumber) {
        $baselines += [ordered]@{ label = 'Latest stable'; short = 'stable'; candidates = @([ordered]@{ version = $StableBuildNumber; buildNumber = $StableBuildNumber }) }
    }
    else {
        $stable = $versions | Where-Object { $_ -match '^\d+\.\d+\.\d+$' } | Sort-Object { [version]$_ } | Select-Object -Last 1
        if ($stable) { $baselines += [ordered]@{ label = 'Latest stable'; short = 'stable'; candidates = @([ordered]@{ version = $stable; buildNumber = $stable }) } }
    }
    return $baselines
}

# A version's numbers: those of the CI build that produced it or, for a version built before these tests existed, of a
# build that re-measured its exact commit with them, tagged metrics-baseline-<version>.
function Get-VersionMetrics($candidate, $headers, $api) {
    $found = [ordered]@{ buildId = $null; buildUrl = $null; lanes = $null; remeasured = $false }
    $sources = @(
        [ordered]@{ query = "buildNumber=$([uri]::EscapeDataString($candidate.buildNumber))"; remeasured = $false }
        [ordered]@{ query = "tagFilters=$([uri]::EscapeDataString("metrics-baseline-$($candidate.version)"))"; remeasured = $true }
    )
    foreach ($source in $sources) {
        # No status filter: what matters is the metrics artifact, and a build whose failed lane is being retried is back
        # "in progress" for a while although its other lanes' metrics are already published.
        $builds = @((Invoke-RestMethod -Headers $headers -Uri "$api`?definitions=$DefinitionId&$($source.query)&queryOrder=queueTimeDescending&api-version=7.1").value)
        foreach ($build in $builds) {
            # The version's own build is linked even when it has no numbers.
            if (-not $found.buildId -and -not $source.remeasured) { $found.buildId = $build.id; $found.buildUrl = $build._links.web.href }
            try { $artifact = Invoke-RestMethod -Headers $headers -Uri "$api/$($build.id)/artifacts?artifactName=runtime-tests-metrics&api-version=7.1" }
            catch { continue } # Not found: the build ran before the tests existed.

            $directory = Join-Path ([System.IO.Path]::GetTempPath()) "runtime-tests-metrics-$($build.id)"
            $zip = "$directory.zip"
            Invoke-WebRequest -Headers $headers -Uri $artifact.resource.downloadUrl -OutFile $zip
            Expand-Archive -Path $zip -DestinationPath $directory -Force
            $lanes = Get-LaneMetrics (Join-Path $directory 'runtime-tests-metrics')
            if ($lanes.Count -gt 0) {
                return [ordered]@{ buildId = $build.id; buildUrl = $build._links.web.href; lanes = $lanes; remeasured = $source.remeasured }
            }
        }
    }
    return $found
}

function Get-BaselineMetrics($baseline) {
    $first = $baseline.candidates[0]
    $result = [ordered]@{ label = $baseline.label; short = $baseline.short; version = $first.version; buildId = $null; buildUrl = $null; lanes = $null; remeasured = $false; newerWithout = $null; note = $null }
    if (-not ($CollectionUri -and $ProjectId -and $DefinitionId)) {
        $result.note = 'no CI connection to look its build up'
        return $result
    }

    # The project is public, so the token is only a courtesy; it is absent when the script runs locally.
    $headers = @{}
    if ($env:SYSTEM_ACCESSTOKEN -and -not $env:SYSTEM_ACCESSTOKEN.StartsWith('$(')) { $headers.Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN" }
    $api = "$($CollectionUri.TrimEnd('/'))/$ProjectId/_apis/build/builds"

    try {
        foreach ($candidate in $baseline.candidates) {
            $found = Get-VersionMetrics $candidate $headers $api
            if ($candidate -eq $first) { $result.buildId = $found.buildId; $result.buildUrl = $found.buildUrl }
            if ($found.lanes) {
                $result.version = $candidate.version
                $result.buildId = $found.buildId
                $result.buildUrl = $found.buildUrl
                $result.lanes = $found.lanes
                $result.remeasured = $found.remeasured
                if ($candidate -ne $first) { $result.newerWithout = $first.version }
                return $result
            }
        }
        $result.note = if ($result.buildId) { 'its build ran before these tests existed' } else { "no CI build numbered $($first.buildNumber) was found" }
    }
    catch {
        $result.note = "its build could not be read ($($_.Exception.Message))"
    }
    return $result
}

# How a version is named in the report: linked to the build its numbers come from, and saying when that build
# re-measured the release commit rather than being the build that published it.
function Format-Version($baseline) {
    $text = if ($baseline.buildUrl) { "[$($baseline.version)]($($baseline.buildUrl -replace ' ', '%20'))" } else { $baseline.version }
    if ($baseline.remeasured) { $text += ' (re-measured)' }
    return $text
}

# Lane titles, in reading order: the three desktop platforms first, then their renderer and host variants.
$LaneTitles = [ordered]@{
    'skia-windows'           = 'Windows'
    'skia-linux'             = 'Linux'
    'skia-macos'             = 'macOS'
    'skia-windows-webgpu'    = 'Windows WebGPU'
    'skia-linux-webgpu'      = 'Linux WebGPU'
    'skia-macos-webgpu'      = 'macOS WebGPU'
    'skia-linux-framebuffer' = 'Linux framebuffer'
}

function Get-LaneTitle([string]$lane) {
    if ($LaneTitles.Contains($lane)) { return $LaneTitles[$lane] }
    return $lane
}

function Get-LaneOrder([string]$lane) {
    $index = @($LaneTitles.Keys).IndexOf($lane)
    if ($index -lt 0) { return 100 }
    return $index
}

function Test-OverBudget($metric) {
    return $null -ne $metric -and $null -ne $metric.budget -and $metric.value -gt $metric.budget
}

# Every budgeted metric is a count where lower is better: ▲ / ▼ flag a clear move away from the latest dev on the same
# lane, past run-to-run noise (the damaged area alone varies by about 13% between runs of the same build). Frame rates
# follow the agent's display rate, so for them only crossing the budget counts.
function Get-Change($row, $current, $devMetric) {
    if ($null -eq $current -or $null -eq $devMetric -or $null -eq $row.budget) { return $null }
    if ($row.name.EndsWith('frames-per-second')) {
        $overNow = Test-OverBudget $current
        if ($overNow -eq (Test-OverBudget $devMetric)) { return $null }
        return $(if ($overNow) { '▲' } else { '▼' })
    }
    $difference = $current.value - $devMetric.value
    if ([math]::Abs($difference) -le [math]::Max(0.05, 0.2 * [math]::Abs($devMetric.value))) { return $null }
    return $(if ($difference -gt 0) { '▲' } else { '▼' })
}

function Format-OverBudget([int]$count) {
    if ($count -eq 0) { return '✅ within budget' }
    return "⚠️ $count over budget"
}

function Format-Changes([int]$lower, [int]$higher) {
    if ($lower + $higher -eq 0) { return 'no change' }
    $parts = @()
    if ($higher -gt 0) { $parts += "$higher higher ▲" }
    if ($lower -gt 0) { $parts += "$lower lower ▼" }
    return $parts -join ', '
}

function New-Report($lanes, $baselines) {
    $laneNames = @($lanes.Keys | Sort-Object { Get-LaneOrder $_ }, { $_ })
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
                if (-not $rows.Contains($key)) { $rows[$key] = [ordered]@{ key = $key; name = $metric.name; budget = $metric.budget; description = $null } }
                if (-not $rows[$key].description -and $metric.description) { $rows[$key].description = $metric.description }
            }
        }

        $dev = $baselines | Where-Object { $_.label -eq 'Latest dev' } | Select-Object -First 1
        $versionHeaders = foreach ($b in $baselines) { "$($b.label)<br>$(Format-Version $b)" }

        # One comparison per lane: this PR next to the latest dev and stable on the same lane, like with like.
        $laneReports = foreach ($lane in $laneNames) {
            $over = 0; $lower = 0; $higher = 0
            $lines = [System.Collections.Generic.List[string]]::new()
            foreach ($row in $rows.Values) {
                $current = $lanes[$lane][$row.key]
                $devMetric = if ($dev -and $dev.lanes -and $dev.lanes.Contains($lane)) { $dev.lanes[$lane][$row.key] } else { $null }
                $cell = Format-Metric $current
                $change = Get-Change $row $current $devMetric
                if ($change) {
                    $cell += " $change"
                    if ($change -eq '▲') { $higher++ } else { $lower++ }
                }
                if (Test-OverBudget $current) { $over++ }

                $budget = if ($null -ne $row.budget) { '≤ ' + (Format-Number $row.budget) } else { '–' }
                $baselineCells = foreach ($b in $baselines) {
                    if ($b.lanes -and $b.lanes.Contains($lane)) { Format-Metric $b.lanes[$lane][$row.key] } else { '–' }
                }
                $lines.Add("| ``$($row.name)`` | $budget | $cell | $($baselineCells -join ' | ') |")
            }
            [pscustomobject]@{ lane = $lane; title = (Get-LaneTitle $lane); over = $over; lower = $lower; higher = $higher; lines = $lines }
        }

        # The versions, one line each, so "within budget" is never read as being about another version.
        $main = $laneReports[0]
        $prOver = @($rows.Values | Where-Object { $key = $_.key; @($laneNames | Where-Object { Test-OverBudget $lanes[$_][$key] }).Count -gt 0 }).Count
        $prLine = if ($prOver -eq 0) { "✅ all $($rows.Count) metrics within budget on all $($laneNames.Count) lanes" } else { "⚠️ $prOver of $($rows.Count) metrics over budget on at least one of $($laneNames.Count) lanes" }
        if ($dev -and $dev.lanes) {
            $up = @($laneReports | Where-Object { $_.higher -gt 0 })
            $prLine += if ($up.Count -eq 0) { '; nothing higher than the latest dev' } else { "; **higher than the latest dev on $(($up | ForEach-Object { $_.title }) -join ', ')** ▲" }
        }
        [void]$sb.AppendLine("- **This PR**: $prLine.")
        foreach ($b in $baselines) {
            $build = Format-Version $b
            if ($b.newerWithout) { $build += ", the newest with numbers ($($b.newerWithout) and later have none yet)" }
            if ($b.lanes) {
                $bLanes = @($laneNames | Where-Object { $b.lanes.Contains($_) })
                $where = if ($bLanes.Count -eq 1) { "measured on $(Get-LaneTitle $bLanes[0]) only" } else { "measured on $($bLanes.Count) lanes" }
                if ($b.lanes.Contains($main.lane)) {
                    $mainLane = $main.lane
                    $over = @($rows.Values | Where-Object { Test-OverBudget $b.lanes[$mainLane][$_.key] }).Count
                    [void]$sb.AppendLine("- **$($b.label)** $($build): $(Format-OverBudget $over) on $($main.title) ($where).")
                }
                else {
                    [void]$sb.AppendLine("- **$($b.label)** $($build): $where.")
                }
            }
            else {
                [void]$sb.AppendLine("- **$($b.label)** $($build): no numbers, $($b.note).")
            }
        }
        [void]$sb.AppendLine()
        [void]$sb.AppendLine('Report only for now: nothing here fails the build.')
        [void]$sb.AppendLine()

        # The main comparison: this PR next to the latest dev and stable on the first lane (Windows when it ran).
        [void]$sb.AppendLine("**Compared with the latest published versions** ($($main.title))")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("| Metric | Budget | This PR | $($versionHeaders -join ' | ') |")
        [void]$sb.AppendLine("|---|---|---:|$(($baselines | ForEach-Object { '---:' }) -join '|')|")
        $main.lines | ForEach-Object { [void]$sb.AppendLine($_) }
        [void]$sb.AppendLine()

        # Then the same comparison for every lane, collapsed, with the lane's status in its heading.
        [void]$sb.AppendLine('**Every lane**')
        [void]$sb.AppendLine()
        foreach ($report in $laneReports) {
            $versusDev = if ($dev -and $dev.lanes -and $dev.lanes.Contains($report.lane)) { " · $(Format-Changes $report.lower $report.higher) against the latest dev" } else { '' }
            [void]$sb.AppendLine("<details><summary><b>$($report.title)</b>: $(Format-OverBudget $report.over)$versusDev</summary>")
            [void]$sb.AppendLine()
            [void]$sb.AppendLine("| Metric | Budget | This PR | $($versionHeaders -join ' | ') |")
            [void]$sb.AppendLine("|---|---|---:|$(($baselines | ForEach-Object { '---:' }) -join '|')|")
            $report.lines | ForEach-Object { [void]$sb.AppendLine($_) }
            [void]$sb.AppendLine()
            [void]$sb.AppendLine('</details>')
        }
        [void]$sb.AppendLine()

        # Collapsed, to keep the comment short. The descriptions come from the tests themselves
        # (RuntimeTestMetrics.Record), so a new budget explains itself here without touching this script.
        [void]$sb.AppendLine('<details><summary>What the numbers mean</summary>')
        [void]$sb.AppendLine()
        [void]$sb.AppendLine('- Every value is a **count** measured by the runtime tests, never a timing, and **lower is better**.')
        [void]$sb.AppendLine('- **Budget** is the highest acceptable value. ⚠️ marks a value over it; – means that version or lane has no value.')
        [void]$sb.AppendLine('- ▲ / ▼: this PR is more than 20% higher / lower than the latest dev on the same lane.')
        [void]$sb.AppendLine("- Frame rates follow each lane's display rate (the framebuffer lane has no vsync), so compare them within a lane; they only count as changed when they cross their budget.")
        [void]$sb.AppendLine('- `x` is a ratio, and `/N` is out of N objects tracked.')
        $described = @($rows.Values | Where-Object { $_.description })
        if ($described.Count -gt 0) {
            [void]$sb.AppendLine()
            [void]$sb.AppendLine('| Metric | What it counts |')
            [void]$sb.AppendLine('|---|---|')
            foreach ($row in $described) { [void]$sb.AppendLine("| ``$($row.name)`` | $($row.description -replace '\|', '\|') |") }
        }
        [void]$sb.AppendLine()
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

        $all = @()
        for ($page = 1; $page -le 20; $page++) {
            $response = Invoke-RestMethod -Headers $headers -Uri "$api/$PullRequestNumber/comments?per_page=100&page=$page"
            # Invoke-RestMethod can hand the JSON array over as a single object: enumerate it, or the filter below sees
            # every comment at once and "finds" all of them.
            $comments = @($response | ForEach-Object { $_ })
            $all += $comments
            if ($comments.Count -lt 100) { break }
        }
        # The newest marker comment of this account: older ones may have been hidden as outdated.
        $existing = $all | Where-Object { $_.user.login -eq $login -and $_.body -and $_.body.StartsWith($Marker) } | Select-Object -Last 1

        $payload = @{ body = $body } | ConvertTo-Json
        if ($existing) {
            try {
                Invoke-RestMethod -Headers $headers -Method Patch -Uri "$api/comments/$($existing.id)" -Body $payload -ContentType 'application/json' | Out-Null
                Write-Host "Updated comment $($existing.html_url)"
                return
            }
            catch {
                # Whatever refused the update (permissions, a deleted comment), a new comment beats no report at all.
                Write-Host "Could not update $($existing.html_url) ($($_.Exception.Message)); posting a new comment instead."
            }
        }
        $created = Invoke-RestMethod -Headers $headers -Method Post -Uri "$api/$PullRequestNumber/comments" -Body $payload -ContentType 'application/json'
        Write-Host "Created comment $($created.html_url)"
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
