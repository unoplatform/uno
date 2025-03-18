param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "969829c0aaa8f3341b196e31e2627953d39a9526" #latest release/stable/9.0 branch commit
    "uno.themes"         = "22df5299701fe9a6f96e4414b559f8dfd5789540" #latest release/stable/5.4 branch commit
    "uno.toolkit.ui"     = "790ebb232a216bd07097868c0cc3d896f8f8dc4e" #latest release/stable/6.4 branch commit
    "uno.check"          = "c327cb365d29a2b53911a3ea9f8cd89254f5729d" #latest main commit
    "uno.xamlmerge.task" = "377ce2d9fdeab0d4f0b94a61e008731a40b10220" #latest release/stable/1.33 branch commit
    "figma-docs"         = "842a2792282b88586a337381b2b3786e779973b4" #latest main commit
    "uno.resizetizer"    = "dfcb976c40eb66cb33c5def4c81d2e4e374dc678" #latest main commit
    "uno.uitest"         = "9669fd2783187d06c36dd6a717c1b9f08d1fa29c" #latest master commit
    "uno.extensions"     = "caadf295630fb154d7d75c8d820f75aa8e014d92" #latest release/stable/5.2 branch commit
    "workshops"          = "e3c2a11a588b184d8cd3a6f88813e5615cca891d" #latest master commit
    "uno.samples"        = "e9ccf60d7d830acf7db4108a8aa5a2a1fc90c481" #latest master commit
}

$uno_git_url = "https://github.com/unoplatform/"

if($branches -ne $null)
{
    foreach ($repo in $branches.keys)
    {
        $branch = $branches[$repo]

        $external_docs[$repo] = $branch
    }
}

echo "Current setup:"
$external_docs

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        popd
        popd

        Set-PSDebug -Off

        throw "Exit code must be zero."
    }
}

if (-Not (Test-Path articles\external))
{
    mkdir articles\external -ErrorAction Continue
}

pushd articles\external

# ensure long paths are supported on Windows
git config --global core.longpaths true

$detachedHeadConfig = git config --get advice.detachedHead
git config advice.detachedHead false

# Heads - Release
foreach ($repoPath in $external_docs.keys)
{
    $repoUrl = "$uno_git_url$repoPath"
    $repoBranch = $external_docs[$repoPath]
        
    if (-Not (Test-Path $repoPath))
    {        
        echo "Cloning $repoPath ($repoUrl@$repoBranch)..."
        git clone $repoUrl $repoPath
        Assert-ExitCodeIsZero
    }

    pushd $repoPath

    echo "Checking out $repoUrl@$repoBranch..."
    git fetch
    git checkout --force $repoBranch
    Assert-ExitCodeIsZero

    # if not detached
    if ((git symbolic-ref -q HEAD) -ne $null)
    {
        echo "Resetting to $repoUrl@$repoBranch..."
        git reset --hard origin/$repoBranch
        Assert-ExitCodeIsZero
    }

    popd
}

git config advice.detachedHead $detachedHeadConfig

popd

Set-PSDebug -Off
