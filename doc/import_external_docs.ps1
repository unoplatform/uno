param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "5ef2048d98df307c738186a5339eedcc4665be72"
    "uno.themes"         = "6ce3ce8b0f61a7a017893541e1dd901ab04c906e"
    "uno.toolkit.ui"     = "aa429dea000f01ff269466dbdc80ae90600fa4f3"
    "uno.check"          = "0350ba1b78ee7488d88f093e8f92cc4d9cb055f5"
    "uno.xamlmerge.task" = "7e8ffef206e87dfea90c53805c45e93a7d8c0b46"
    "figma-docs"         = "909bfd2f272b584e1312b64136c7fb98c66e625d"
    "uno.resizetizer"    = "3eec4aad0b7b3480ec6c2a121911ffde844fc4f8"
    "uno.uitest"         = "555453c2985ef2745fe44503c5809a6168d063c2"
    "uno.extensions"     = "0cbe572c56e956bbf3d51cb1d51f36f513e0402f"
    "workshops"          = "f3d80513c7a9a69162bedd6a145462a3a4310f33"
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
