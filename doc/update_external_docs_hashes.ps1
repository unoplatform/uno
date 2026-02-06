param(
    [Parameter(Mandatory = $false)]
    [string]$Branch,

    [Parameter(Mandatory = $false)]
    [string]$GitHubToken
)

Write-Host "Updating external docs commit hashes..." -ForegroundColor Green

# Use the provided GitHub token (if any) to authenticate API calls
$githubToken = if ($GitHubToken) { $GitHubToken } else { $env:GITHUB_TOKEN }
$baseHeaders = @{
    "Accept"     = "application/vnd.github.v3+json"
    "User-Agent" = "Uno-Docs-Updater"
}
if (-not [string]::IsNullOrEmpty($githubToken)) {
    $baseHeaders["Authorization"] = "Bearer $githubToken"
}

function Get-GitHubRateLimitMessage {
    param(
        [Parameter(Mandatory = $true)]
        [Object]$Exception
    )

    $response = $null
    if ($Exception -and $Exception.PSObject.Properties['Response']) {
        $response = $Exception.Response
    }

    if (-not $response) {
        return $null
    }

    $statusCode = $null
    try { $statusCode = [int]$response.StatusCode } catch {}

    if ($statusCode -ne 403) {
        return $null
    }

    $remaining = $response.Headers['X-RateLimit-Remaining']
    $limit     = $response.Headers['X-RateLimit-Limit']
    $resetUnix = $response.Headers['X-RateLimit-Reset']

    if ($remaining -ne '0') {
        return $null
    }

    $resetLocal = $null
    if ($resetUnix) {
        try {
            $resetLocal = [DateTimeOffset]::FromUnixTimeSeconds([int64]$resetUnix).ToLocalTime()
        } catch {}
    }

    if ($resetLocal) {
        return "GitHub API rate limit exceeded (limit=$limit, remaining=$remaining). Limit resets at $resetLocal."
    } else {
        return "GitHub API rate limit exceeded (limit=$limit, remaining=$remaining)."
    }
}

# Resolve path to import script relative to this script's location
$scriptPath = Join-Path $PSScriptRoot "import_external_docs.ps1"

if (-Not (Test-Path $scriptPath)) {
    Write-Error "Script not found: $scriptPath"
    exit 1
}

# Get the list of repositories from the script
Write-Host "Getting repository list from $scriptPath..." -ForegroundColor Cyan
$repos = & $scriptPath -ListRepos

if (-not $repos -or $repos.Count -eq 0) {
    Write-Warning "No repositories found in $scriptPath"
    exit 0
}

Write-Host "Found $($repos.Count) repositories to update:" -ForegroundColor Green
$repos | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }

# Determine behavior based on the provided branch
$branchInput = $Branch
if (-not $branchInput) {
    $branchInput = 'master'
}

$useReleaseBranches = $branchInput -like 'release/stable/*'

if ($useReleaseBranches) {
    Write-Host "Running in release mode for '$branchInput' - will use each repo's latest 'release/stable/*' branch if available, otherwise its default branch." -ForegroundColor Cyan
} else {
    Write-Host "Running in default mode for '$branchInput' - will use each repo's default branch." -ForegroundColor Cyan
}

# Read the current script content
$content = Get-Content $scriptPath -Raw
$updatedCount = 0

foreach ($repo in $repos) {
    Write-Host "Processing $repo..." -ForegroundColor Cyan

    try {
        # First, get the repository info to find the default branch
        $repoInfoUrl = "https://api.github.com/repos/unoplatform/$repo"
        $repoInfo = Invoke-RestMethod -Uri $repoInfoUrl -Headers $baseHeaders

        $defaultBranch = $repoInfo.default_branch
        Write-Host "  Default branch: $defaultBranch" -ForegroundColor Gray

        $targetBranch = $defaultBranch
        $branchDescription = $defaultBranch

        if ($useReleaseBranches) {
            # Try to locate the latest release/stable/* branch for this repo
            $branchesUrl = "https://api.github.com/repos/unoplatform/$repo/branches?per_page=100"

            # Explicitly handle pagination to ensure all branches are fetched.
            # The GitHub API returns a maximum of 100 branches per page.
            # This loop follows the 'next' link in the response headers until all pages are retrieved.
            $branches = @()
            $nextUrl = $branchesUrl
            do {
                $response = Invoke-WebRequest -Uri $nextUrl -Headers $baseHeaders
                $pageBranches = $response.Content | ConvertFrom-Json
                $branches += $pageBranches
                $linkHeader = $response.Headers['Link']
                $nextUrl = $null
                if ($linkHeader) {
                    # Parse the Link header to find the next page URL
                    foreach ($link in $linkHeader -split ',') {
                        if ($link -match '<(.*?)>; rel="next"') {
                            $nextUrl = $matches[1]
                            break
                        }
                    }
                }
            } while ($nextUrl)
            $releaseBranches = @()
            if ($branches) {
                foreach ($b in @($branches)) {
                    $name = $b.name
                    if ($name -like 'release/stable/*') {
                        $suffix = $name.Substring('release/stable/'.Length)
                        try {
                            $version = [version]$suffix
                            $releaseBranches += [pscustomobject]@{ Name = $name; Version = $version }
                        } catch {
                            Write-Host "  Skipping branch '$name' - unable to parse version part '$suffix'." -ForegroundColor DarkYellow
                        }
                    }
                }
            }

            if ($releaseBranches.Count -gt 0) {
                $latestRelease = $releaseBranches | Sort-Object Version -Descending | Select-Object -First 1
                $targetBranch = $latestRelease.Name
                $branchDescription = $latestRelease.Name
                Write-Host "  Using latest release branch: $targetBranch" -ForegroundColor Gray
            } else {
                Write-Host "  No 'release/stable/*' branches found - falling back to default branch '$defaultBranch'." -ForegroundColor DarkYellow
            }
        }

        # Get the latest commit hash from the selected branch
        $commitUrl = "https://api.github.com/repos/unoplatform/$repo/commits/$targetBranch"
        $commitInfo = Invoke-RestMethod -Uri $commitUrl -Headers $baseHeaders

        $latestHash = $commitInfo.sha
        Write-Host "  Latest commit on '$targetBranch': $latestHash" -ForegroundColor Gray

        # Update only the hash value while preserving existing spacing, comments, and other keys.
        $escapedRepo = [regex]::Escape($repo)
        # The hash pattern matches either:
        #   - 7 to 40 lowercase hex chars (abbreviated/full SHA-1, as supported by Git)
        #   - exactly 64 lowercase hex chars (full SHA-256, as GitHub is transitioning to SHA-256)
        # This ensures forward compatibility while avoiding accidental matches of partial hashes.
        $hashPattern = "(?:[a-f0-9]{7,40}|[a-f0-9]{64})"
        $pattern = "(`"$escapedRepo`"\s*=\s*@\{\s*ref\s*=\s*`" )$hashPattern(`")"

        $newContent = [regex]::Replace($content, $pattern, {
                param($match)
                $match.Groups[1].Value + $latestHash + $match.Groups[2].Value
            })

        if ($newContent -ne $content) {
            $content = $newContent
            $updatedCount++
            Write-Host "   Updated $repo to $latestHash" -ForegroundColor Green
        } else {
            Write-Host "   No change for $repo (already up to date or pattern not matched)" -ForegroundColor Yellow
        }
    }
    catch {
        $rateMsg = Get-GitHubRateLimitMessage -Exception $_.Exception
        if ($rateMsg) {
            Write-Error "Rate limit hit while processing $repo. $rateMsg"
        } else {
            Write-Warning "Failed to process $repo : $_"
        }
    }
}

# Write the updated content back to the file
if ($updatedCount -gt 0) {
    Set-Content -Path $scriptPath -Value $content -NoNewline
    Write-Host "`n Updated $updatedCount commit hash(es) in $scriptPath" -ForegroundColor Green
} else {
    Write-Host "`n No updates were made" -ForegroundColor Yellow
}

# Show git diff for verification
Write-Host "`nChanges made:" -ForegroundColor Cyan
# Use relative path for nicer git output if possible
$relativePath = "doc/import_external_docs.ps1"
if (Test-Path $relativePath) {
    git diff $relativePath
} else {
    git diff $scriptPath
}
