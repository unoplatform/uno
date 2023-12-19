param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "5ef2048d98df307c738186a5339eedcc4665be72"
    "uno.themes"         = "37e913dea361bcc13e20587184bb3691c6b31d3b"
    "uno.toolkit.ui"     = "38838426c54a8465873442c7f677de457f19adbc"
    "uno.check"          = "ec524c077487a6156674c9f18fbe073eae025bbf"
    "uno.xamlmerge.task" = "7e8ffef206e87dfea90c53805c45e93a7d8c0b46"
    "figma-docs"         = "1fb802a63fdb337657147dfb679537ebf568f47f"
    "uno.resizetizer"    = "3eec4aad0b7b3480ec6c2a121911ffde844fc4f8"
    "uno.uitest"         = "555453c2985ef2745fe44503c5809a6168d063c2"
    "uno.extensions"     = "028fdc670d56abf3f4fa7e39ad5f5e0ce225a906"
    "workshops"          = "0c964f38df5a4b0f32cf36cd74591df3012b8e20"
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
