param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
<<<<<<< HEAD
    "uno.wasm.bootstrap" = "a5e5bd494119c254ad503ed3e61970b95eca4037" #latest main commit
=======
    "uno.wasm.bootstrap" = "dd4eea23dc3e79372f4b7a9399d353fcfc01a53e" #latest main commit
>>>>>>> 49ab71ffdd (docs: Update documentation with latest changes)
    "uno.themes"         = "9ea2ee38186b55745cd211bcf628b06821067465" #latest release branch commit
    "uno.toolkit.ui"     = "438f84ac73d687d6ec4246002c7cb4295a7e4809" #latest release branch commit
    "uno.check"          = "4a7dd7290daf0aabfbb8efabcd2b067898b7f45e" #latest main commit
    "uno.xamlmerge.task" = "21f02c98702b875a9942047ca042e41810b6fe56" #latest main commit
    "figma-docs"         = "529c1e3c67cb350f2e4b8969d76f6501d9ec8cb0" #latest main commit
    "uno.resizetizer"    = "d5659362b965a48e2fb07e0ed7845bb6deefbf3f" #latest main commit
    "uno.uitest"         = "9669fd2783187d06c36dd6a717c1b9f08d1fa29c" #latest master commit
    "uno.extensions"     = "a527c0412d13c1c5f5c42e2c9e755860ce13d348" #latest release branch commit
    "workshops"          = "6f56d41eb7f76e5af7dbda88928cd28999cdf6d8" #latest master commit
    "uno.samples"        = "275f289d31b83c7bd7469c328d63d001c0dc140f" #latest master commit
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
