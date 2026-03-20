param(
    [Parameter(ValueFromPipelineByPropertyName = $true)]$branches = $null,
    [Parameter(ValueFromPipelineByPropertyName = $true)][string]$contributor_git_url = $null,
    [Parameter(ValueFromPipelineByPropertyName = $true)][string[]]$forks_to_import = $null
    [switch]$ListRepos
)

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> @{ ref = '<commit|branch>'; dest = '<sub-folder>'? }
$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = @{ ref="b0a18b301d446ed0ae38839c425bf354aa19fc4b" }  #latest main commit
    "uno.themes"         = @{ ref="1d7d5b4d595e04f1052d40d14e09f614a66bfcf4" }  #latest master commit
    "uno.toolkit.ui"     = @{ ref="bcf49ce80b72a027903217ac6f0cd4b91dd09164" }  #latest main commit
    "uno.check"          = @{ ref="21062486af894a55f75e060cffbd91407b669e1e" }  #latest main commit
    "uno.xamlmerge.task" = @{ ref="081dcfa44b5ce24ac0948675e5ee6b781e2107bc" }  #latest main commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" }  #latest main commit
    "uno.resizetizer"    = @{ ref="a459d04b4b6298ee086f7c3ed8bbd0c5d038d6be" }  #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" }  #latest master commit
    "uno.extensions"     = @{ ref="8bf0e80c80a1c1ba1050eede12ea0aa3317e7b6e" }  #latest main commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" }  #latest master commit
    "uno.samples"        = @{ ref="1d9ea60a7aec335e1d034446c631b93f605f06b8" }  #latest master commit
    "uno.chefs"          = @{ ref="06f4f8042595473557f00cdfa622788273d3a131" }  #latest main commit
    "hd-docs"            = @{ ref="ec0553b7a2d000cc0138c020215f313a04ec8807"; dest="studio/Hot Design" } #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

# --- END OF CONFIGURATION --------------------------------------------------

# Validation ordering note: contributor URL normalization must occur BEFORE requiring it when forks are provided.

# Normalize blank contributor URL to null (CI may pass empty string)
if ([string]::IsNullOrWhiteSpace($contributor_git_url)) { $contributor_git_url = $null }

# If forks are specified and non-empty, contributor_git_url must be present (non-null after normalization)
if ($forks_to_import -ne $null -and $forks_to_import.Count -gt 0 -and $contributor_git_url -eq $null) {
    throw "Parameter 'forks_to_import' requires 'contributor_git_url' to be specified."
}

# Validate fork names against configured repositories (only if array provided)
if ($forks_to_import -ne $null) {
    $configuredRepos = $external_docs.Keys | ForEach-Object { $_.ToLower() }
    $invalidForks = $forks_to_import |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Where-Object { $configuredRepos -notcontains $_.ToLower() }
    if ($invalidForks.Count -gt 0) {
        throw "The following repository names in forks_to_import are not configured in external_docs: $($invalidForks -join ', ')"
    }
}

# If a contributor git URL is provided, validate only when forks are specified; never fail CI when unused.
if ($contributor_git_url -ne $null) {
    if ($forks_to_import -eq $null -or $forks_to_import.Count -eq 0) {
        Write-Warning 'Parameter ''contributor_git_url'' was provided but ''forks_to_import'' is null or empty. The contributor URL will not be used.'
    }
    else {
        if ($contributor_git_url -notmatch '^https?://') {
            Write-Warning "Parameter 'contributor_git_url' is not a valid HTTP/HTTPS URL. Ignoring contributor URL and falling back to Uno repos."
            $contributor_git_url = $null
        }
        elseif (-not $contributor_git_url.EndsWith('/')) {
            $contributor_git_url += '/'
        }
    }

# If -ListRepos flag is set, output the repository names and exit
if ($ListRepos) {
    Set-PSDebug -Off
    $external_docs.Keys | ForEach-Object { Write-Output $_ }
    exit 0

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

    if ($contributor_git_url -ne $null -and $forks_to_import -ne $null -and ($forks_to_import -contains $repoPath)) {
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
