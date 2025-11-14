param(
    [Parameter(ValueFromPipelineByPropertyName = $true)]$branches = $null,
    [Parameter(ValueFromPipelineByPropertyName = $true)][string]$contributor_git_url = $null,
    [Parameter(ValueFromPipelineByPropertyName = $true)][string[]]$forks_to_import = $null
)

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> @{ ref = '<commit|branch>'; dest = '<sub-folder>'? }
$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = @{ ref="401557af53854da72beb2a32cd5b874d768a0702" } #latest release/stable/10.0 branch commit
    "uno.themes"         = @{ ref="6dbbe45ef852fabeff2c41d134cbde6f2ef6bc96" } #latest release/stable/6.0 branch commit
    "uno.toolkit.ui"     = @{ ref="915bf060ea6f059b7b48aeb0c052b021855b732a" } #latest release/stable/8.3 branch commit
    "uno.check"          = @{ ref="3b3907bdc3ffe2ae4dd03e96b2e7db2679d9e290" } #latest release/stable/1.33 branch commit
    "uno.xamlmerge.task" = @{ ref="377ce2d9fdeab0d4f0b94a61e008731a40b10220" } #latest release/stable/1.33 branch commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" } #latest main commit
    "uno.resizetizer"    = @{ ref="e051343230e86d2e4ebc5e1840e530dd4fc9a4da" } #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" } #latest master commit
    "uno.extensions"     = @{ ref="4e253164e577a014152d94f239cb81de284441a9" } #latest release/stable/7.0 branch commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" } #latest master commit
    "uno.samples"        = @{ ref="8098a452951c9f73cbcf8d0ac1348f029820e53a" } #latest master commit
    "uno.chefs"          = @{ ref="af0a0c928337688c5ed3e87c3389a1cfdad46933" } #latest main commit
    "hd-docs"            = @{ ref="ded00dc100ae7dcba4a78fd32d393a58c1d1f23e"; dest="studio/Hot Design" } #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

# --- END OF CONFIGURATION --------------------------------------------------

# If branches are passed, use them to override the default ones (ref, but not dest)

if ($contributor_git_url -ne $null -and -not ($contributor_git_url -is [string])) {
    throw "The parameter 'contributor_git_url' must be a string or null."
}
if ($forks_to_import -ne $null -and -not ($forks_to_import -is [string[]])) {
    throw "The parameter 'forks_to_import' must be a array of string or null."
}

# --- ADDITIONAL VALIDATION (review comments) ---------------------------------
# If a contributor git URL is provided but no forks are specified, warn the user.
if ($contributor_git_url -and (-not $forks_to_import -or $forks_to_import.Count -eq 0)) {
    Write-Warning "Parameter 'contributor_git_url' was provided but 'forks_to_import' is null or empty. The contributor URL will not be used."
}

# Validate that all entries in forks_to_import exist in the external_docs map.
if ($forks_to_import) {
    $invalidForks = @()
    foreach ($fork in $forks_to_import) {
        # Use case-insensitive comparison against configured repo keys.
        if (-not ($external_docs.Keys -contains $fork)) {
            $invalidForks += $fork
        }
    }
    if ($invalidForks.Count -gt 0) {
        throw "The following repository names in forks_to_import are not configured in external_docs: $($invalidForks -join ', ')"
    }
}

# If branches are passed, use them to override the default ones (ref, but not dest)
if ($branches -ne $null) {
    foreach ($repo in $branches.Keys) {
        $branch = $branches[$repo]
        if ($external_docs[$repo]) {
            $external_docs[$repo].ref = $branch
        }
        else {
            $external_docs[$repo] = @{ ref = $branch }     # default dest = external
        }
    }
}

echo "Current setup:"
$external_docs

$ErrorActionPreference = 'Stop'

function Get-TargetRoot([string]$repo) {
    $cfg = $external_docs[$repo]
    $dest = if ($cfg.ContainsKey('dest') -and $cfg.dest) { $cfg.dest } else {
        Join-Path 'external' $repo 
    }
    return Join-Path 'articles' $dest
}

function Assert-ExitCodeIsZero() {
    if ($LASTEXITCODE -ne 0) {
        Set-PSDebug -Off

        throw "Exit code must be zero."
    }
}

# ensure long paths are supported on Windows
git config --global core.longpaths true

$detachedHeadConfig = git config --get advice.detachedHead
git config advice.detachedHead false

# Heads - Release
foreach ($repoPath in $external_docs.Keys) {
    $repoCfg = $external_docs[$repoPath]
    $repoRef = $repoCfg.ref
    $targetRoot = Get-TargetRoot $repoPath
    $fullPath = $targetRoot

    if ($forks_to_import -and ($forks_to_import -contains $repoPath)) {
        # Fork override: use contributor-provided git URL base
        $repoUrl = "$contributor_git_url$repoPath"
    }
    else {
        $repoUrl = "$uno_git_url$repoPath"
    }

    Write-Output "Importing $repoPath from $repoUrl..."

    if (-Not (Test-Path $fullPath)) {        
        Write-Host "Cloning $repoPath ($repoUrl@$repoRef) into $targetRoot..." -ForegroundColor Black -BackgroundColor Blue
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $fullPath) | Out-Null
        git clone --filter=blob:none --no-tags $repoUrl $fullPath
        Assert-ExitCodeIsZero
    }
    else {
        Write-Host "Skipping clone of $repoPath ($repoUrl@$repoRef) into $targetRoot (already exists)."-ForegroundColor Black -BackgroundColor DarkYellow
    }
    pushd $fullPath


    try {
        Write-Host "Checking out $repoUrl@$repoRef..." -ForegroundColor Black -BackgroundColor Blue
        git fetch --filter=blob:none origin $repoRef # fetch the latest commit for the specified ref
        Assert-ExitCodeIsZero
        git checkout --detach FETCH_HEAD # detach the HEAD to avoid issues with git status
        Assert-ExitCodeIsZero

        # if not detached
        if ((git symbolic-ref -q HEAD) -ne $null) {
            echo "Resetting to $repoUrl@$repoRef..."
            git reset --hard origin/$repoRef
            Assert-ExitCodeIsZero
        }
    }
    finally {
        popd
    }
}

git config advice.detachedHead $detachedHeadConfig

Set-PSDebug -Off
