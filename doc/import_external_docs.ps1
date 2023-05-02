param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "4abadfc93ffeddc82420cc28af04cd7f6b2693ab"
    "uno.themes"         = "3d12f341f3ce9ecd7738e163a3a0904e9b94466f"
    "uno.toolkit.ui"     = "434712b657f479d1329ff60af3b6f22bb6fdb34c"
    "uno.check"          = "5dec33b3cb4c26f578c8d6bd7a84000bf265a14e"
    "uno.xamlmerge.task" = "7e8ffef206e87dfea90c53805c45e93a7d8c0b46"
    "figma-docs"         = "f13d08f2bd7b62fc274b43a4ede4d75909d0f41f"
    "uno.resizetizer"    = "f650e844cb66a7dafa084a7e778f14f4fd3ce8a2"
    "uno.uitest"         = "555453c2985ef2745fe44503c5809a6168d063c2"
    "uno.extensions"     = "539d6b0f2e61fbc2ae5d6e35a77de41cafacf5ce"      
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
    git checkout $repoBranch
    Assert-ExitCodeIsZero

    # if not detached
    if ((git symbolic-ref -q HEAD) -ne $null)
    {
        echo "Pulling $repoUrl@$repoBranch..."
        git pull
        Assert-ExitCodeIsZero
    }

    popd
}

git config advice.detachedHead $detachedHeadConfig

popd

Set-PSDebug -Off
