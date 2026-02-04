param(
    [string]$Project,
    [string]$DefinitionId
)

$url = "$($env:SYSTEM_COLLECTIONURI)$Project/_apis/build/builds?definitions=$DefinitionId&branchName=refs/heads/master&statusFilter=completed&queryOrder=finishTimeDescending&api-version=6.0"

try {
    $response = Invoke-RestMethod -Uri $url -Method Get
    $builds = $response.value

    if (-not $builds -or $builds.Count -eq 0) {
        Write-Host "No builds found on master."
        exit 0
    }

    function Test-BuildSucceeded($build) {
        if ($build.result -eq "succeeded" -or $build.result -eq "partiallySucceeded") {
            return $true
        }

        $publishIssues = $build.issues |
            Where-Object { $_.type -eq "error" -and $_.message -match "Publish to nuget\.org" }

        if ($publishIssues.Count -gt 0) {
            Write-Host "Build $($build.buildNumber) failed only due to 'Publish to nuget.org'. Treating as succeeded."
            return $true
        }

        return $false
    }

    $latestBuild = $builds[0]
    if (Test-BuildSucceeded $latestBuild) {
        Write-Host "Latest master build $($latestBuild.buildNumber) succeeded."
        exit 0
    }

    Write-Host "Latest master build $($latestBuild.buildNumber) failed."

    $lastSuccess = $builds |
        Where-Object { Test-BuildSucceeded $_ } |
        Select-Object -First 1

    if ($null -eq $lastSuccess) {
        Write-Host "No successful builds found in history."
        exit 0
    }

    $lastSuccessTime = [DateTime]$lastSuccess.finishTime
    $now = [DateTime]::UtcNow
    $diff = $now - $lastSuccessTime

    Write-Host "Last successful build was $($lastSuccess.buildNumber) at $lastSuccessTime ($([Math]::Round($diff.TotalHours, 2)) hours ago)."

    if ($diff.TotalHours -gt 24) {
        Write-Host "Master has been failing for more than 24 hours. Last success was at $lastSuccessTime. Blocking PR merge."
        exit 1
    }

    Write-Host "Master is failing, but within the 24h grace period."
    exit 0
}
catch {
    Write-Error "Failed to check master status: $($_.Exception.Message)"
    exit 1
}
