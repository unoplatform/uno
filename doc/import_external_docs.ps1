param(
  [Parameter(ValueFromPipelineByPropertyName = $true)]
  $branches = $null,
  [switch]$ListRepos
)

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> @{ ref = '<commit|branch>'; dest = '<sub-folder>'? }
$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = @{ ref="85316daac1891b301519b57cd3c7a6717c9ef9b8" } #latest main commit
    "uno.themes" = @{ ref="f1e5b642974ea8d3b887049019116b86d0adccf1" } #latest master commit
    "uno.toolkit.ui" = @{ ref="139ae344c61c937ffd84ae27203fc8215a289acf" } #latest main commit
    "uno.check" = @{ ref="faa9fa0aab4532b863a4fb44841a78f17c6339f3" } #latest main commit
    "uno.xamlmerge.task" = @{ ref="7f3fc6a037ea46ed16963e5551d4d0802acc7114" } #latest main commit
    "figma-docs" = @{ ref="842a2792282b88586a337381b2b3786e779973b4" } #latest main commit
    "uno.resizetizer" = @{ ref="e051343230e86d2e4ebc5e1840e530dd4fc9a4da" } #latest main commit
    "uno.uitest" = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" } #latest master commit
    "uno.extensions" = @{ ref="6eb6c93f04346dd8640c891c77989bf872afce3b" } #latest main commit
    "workshops" = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" } #latest master commit
    "uno.samples" = @{ ref="8098a452951c9f73cbcf8d0ac1348f029820e53a" } #latest master commit
    "uno.chefs" = @{ ref="16a62fdd6950a2f89fe0f94dc5cc67207cab9c4a" } #latest main commit
    "hd-docs"            = @{ ref="ded00dc100ae7dcba4a78fd32d393a58c1d1f23e"; dest="studio/Hot Design" } #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

# --- END OF CONFIGURATION --------------------------------------------------

# If -ListRepos flag is set, output the repository names and exit
if ($ListRepos) {
    Set-PSDebug -Off
    $external_docs.Keys | ForEach-Object { Write-Output $_ }
    exit 0
}

# If branches are passed, use them to override the default ones (ref, but not dest)
if ($branches) {
    foreach ($repo in $branches.Keys) {
        $branch = $branches[$repo]
        if ($external_docs[$repo]) {
            $external_docs[$repo].ref = $branch
        } else {
            $external_docs[$repo] = @{ ref = $branch }     # default dest = external
        }
    }
}

echo "Current setup:"
$external_docs

$ErrorActionPreference = 'Stop'

function Get-TargetRoot([string]$repo) {
    $cfg  = $external_docs[$repo]
    $dest = if ($cfg.ContainsKey('dest') -and $cfg.dest) { $cfg.dest } else { Join-Path 'external' $repo }
    return Join-Path 'articles' $dest
}

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        Set-PSDebug -Off

        throw "Exit code must be zero."
    }
}

# ensure long paths are supported on Windows
git config --global core.longpaths true

$detachedHeadConfig = git config --get advice.detachedHead
git config advice.detachedHead false

# Heads - Release
foreach ($repoPath in $external_docs.Keys)
{
    $repoCfg = $external_docs[$repoPath]
    $repoRef = $repoCfg.ref
    $targetRoot = Get-TargetRoot $repoPath
    $fullPath   = $targetRoot
    $repoUrl = "$uno_git_url$repoPath"
        
    if (-Not (Test-Path $fullPath))
    {        
        Write-Host "Cloning $repoPath ($repoUrl@$repoRef) into $targetRoot..." -ForegroundColor Black -BackgroundColor Blue
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $fullPath) | Out-Null
        git clone --filter=blob:none --no-tags $repoUrl $fullPath
        Assert-ExitCodeIsZero
    }
    else
    {
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
        if ((git symbolic-ref -q HEAD) -ne $null)
        {
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
