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
<<<<<<< HEAD
    "uno.wasm.bootstrap" = @{ ref="90d991a0ff12d3c6a3a8baf61ececb0b3e5d8346" } #latest release/stable/10.0 branch commit
    "uno.themes"         = @{ ref="1f70ae895eba4cea1658e4c8d05c99b9ad051893" } #latest release/stable/6.1 branch commit
    "uno.toolkit.ui"     = @{ ref="347d5a141e692a17c446b39b7a7e899806da1d59" } #latest release/stable/8.4 branch commit
    "uno.check"          = @{ ref="0fabc47c022c7eb86b1c4760441780ff3638b06e" } #latest release/stable/1.33 branch commit
    "uno.xamlmerge.task" = @{ ref="377ce2d9fdeab0d4f0b94a61e008731a40b10220" } #latest release/stable/1.33 branch commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" } #latest main commit
    "uno.resizetizer"    = @{ ref="afba34d65a8077e8c29e78c5fda416ab4928d8df" } #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" } #latest master commit
    "uno.extensions"     = @{ ref="ce51198a52243ae9ee0e0777e4abbc90b9e142e1" } #latest release/stable/7.1 branch commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" } #latest master commit
    "uno.samples"        = @{ ref="8098a452951c9f73cbcf8d0ac1348f029820e53a" } #latest master commit
    "uno.chefs"          = @{ ref="06f4f8042595473557f00cdfa622788273d3a131" } #latest main commit
    "hd-docs"            = @{ ref="03df0e2a08dde9527b72ac58e1fe3b2d402a38e3"; dest="studio/Hot Design" } #latest main commit
=======
    "uno.wasm.bootstrap" = @{ ref="817a4fe1a2e333c22e08477c5ee738d3f144cc8d" }  #latest main commit
    "uno.themes"         = @{ ref="02f3c86ec8d7426950460f2739b501a660e1ccd3" }  #latest master commit
    "uno.toolkit.ui"     = @{ ref="b8ebddb96cc030156aa765d0706ee978d4b3f5e2" }  #latest main commit
    "uno.check"          = @{ ref="ff69cd013f9c84da278cffdac8f4644363d5361d" }  #latest main commit
    "uno.xamlmerge.task" = @{ ref="081dcfa44b5ce24ac0948675e5ee6b781e2107bc" }  #latest main commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" }  #latest main commit
    "uno.resizetizer"    = @{ ref="e422ad9f26cf21ed02c339e717e0dd0189bb566e" }  #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" }  #latest master commit
    "uno.extensions"     = @{ ref="0e9983942842aa28bb4849b2cb133b6480eb54c2" }  #latest main commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" }  #latest master commit
    "uno.samples"        = @{ ref="1d9ea60a7aec335e1d034446c631b93f605f06b8" }  #latest master commit
    "uno.chefs"          = @{ ref="d54bceea13406bca23e870a89ecee469813c69b3" }  #latest main commit
    "hd-docs"            = @{ ref="ec0553b7a2d000cc0138c020215f313a04ec8807"; dest="studio/Hot Design" } #latest main commit
<<<<<<< HEAD
    "studio-docs"        = @{ ref="5253bed114df4180f3f05f458f977db9e92ea680" }  #latest main commit
>>>>>>> 442dc5bd25 (docs: Import studio-docs and wire Studio App/Agent toc)
=======
    "studio-docs"        = @{ ref="97f349bc1cb3b0ecb9cfdc2fa86c5aac137ba4ee" }  #latest main commit
>>>>>>> e9f10c9eda (docs: bump studio-docs pin to include App/Agent)
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
