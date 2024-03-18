param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
<<<<<<< HEAD
    "uno.wasm.bootstrap" = "5ef2048d98df307c738186a5339eedcc4665be72"
    "uno.themes"         = "6ce3ce8b0f61a7a017893541e1dd901ab04c906e"
    "uno.toolkit.ui"     = "62a2ec7fb777bf40da7af2ea8fc83576f680e275"
    "uno.check"          = "0350ba1b78ee7488d88f093e8f92cc4d9cb055f5"
    "uno.xamlmerge.task" = "7e8ffef206e87dfea90c53805c45e93a7d8c0b46"
    "figma-docs"         = "909bfd2f272b584e1312b64136c7fb98c66e625d"
    "uno.resizetizer"    = "3eec4aad0b7b3480ec6c2a121911ffde844fc4f8"
    "uno.uitest"         = "555453c2985ef2745fe44503c5809a6168d063c2"
    "uno.extensions"     = "f154d50ca97f5ebf530b299d3ecb30f36604100a"
    "workshops"          = "f3d80513c7a9a69162bedd6a145462a3a4310f33"
=======
    "uno.wasm.bootstrap" = "b1bc52b211bfe73e75a285e6fa1c89c7f655c335" #latest main commit
    "uno.themes"         = "fa2eee00b63901c94840633204e0837b32ade719" #latest release branch commit
    "uno.toolkit.ui"     = "d48202c41792148db7ea1b05d5a9e42c651f6218" #latest release branch commit
    "uno.check"          = "e84be1813874a3b876a74a911a6fee1430092b87" #latest main commit
    "uno.xamlmerge.task" = "21f02c98702b875a9942047ca042e41810b6fe56" #latest main commit
    "figma-docs"         = "9106b3228677b7025704dbaa02ff196da6b2fd3e" #latest main commit
    "uno.resizetizer"    = "f47b65ffbfca58dbccf1e8cb2c35cfeaeb8cc20a" #latest main commit
    "uno.uitest"         = "9669fd2783187d06c36dd6a717c1b9f08d1fa29c" #latest master commit
    "uno.extensions"     = "f154d50ca97f5ebf530b299d3ecb30f36604100a" #latest release branch commit
    "workshops"          = "f3d80513c7a9a69162bedd6a145462a3a4310f33" #latest master commit
>>>>>>> 4cd5bce817 (docs: Update documentation to latest commits)
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
