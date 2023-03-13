Set-PSDebug -Trace 1

$external_docs =
@(
    @("https://github.com/unoplatform/uno.wasm.bootstrap", "uno.wasm.bootstrap", "4abadfc93ffeddc82420cc28af04cd7f6b2693ab"),
<<<<<<< HEAD
    @("https://github.com/unoplatform/uno.themes", "uno.themes", "1d825c6fb599aeaffc52229d102949a3b7452b94"),
    @("https://github.com/unoplatform/uno.toolkit.ui", "uno.toolkit.ui", "3edaf9fb29e95a8c4a24b5b43d273dd85bc1fa57"),
    @("https://github.com/unoplatform/uno.check", "uno.check", "e1b260c2e112b7dd92ed77ff46020dd265cd6b90"),
    @("https://github.com/unoplatform/uno.xamlmerge.task", "uno.xamlmerge.task", "a6d2efa69e24e8280c38300b5c1b7a8f2033f9f9"),
=======
    @("https://github.com/unoplatform/uno.themes", "uno.themes", "3e8e7dc8e816379d335cd13d11bd90227c7fcc44"),
    @("https://github.com/unoplatform/uno.toolkit.ui", "uno.toolkit.ui", "d512cc88a541b2da858e7013b81d8a1b83d89272"),
    @("https://github.com/unoplatform/uno.check", "uno.check", "5dec33b3cb4c26f578c8d6bd7a84000bf265a14e"),
    @("https://github.com/unoplatform/uno.xamlmerge.task", "uno.xamlmerge.task", "7e8ffef206e87dfea90c53805c45e93a7d8c0b46"),
>>>>>>> ece6592b9f (docs: Update xamlmerge and uno.check)
    @("https://github.com/unoplatform/figma-docs", "figma-docs", "a740582020509f9947fbf991628075a4717bff0a"),
    @("https://github.com/unoplatform/uno.extensions", "uno.extensions", "b9c1b66ff45776b0bc9c5a2cb588fe3451046d1e")
)

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
    }
}

mkdir articles\external -ErrorAction Continue
pushd articles\external

# ensure long paths are supported on Windows
git config --global core.longpaths true

# Heads - Release
for ($i = 0; $i -lt $external_docs.Length; $i++)
{
    $repoUrl=$external_docs[$i][0]
    $repoPath=$external_docs[$i][1]
    $repoBranch=$external_docs[$i][2]

    echo "Cloning $repoPath ($repoUrl@$repoBranch)"

    git clone $repoUrl $repoPath
    Assert-ExitCodeIsZero

    pushd $repoPath
    git checkout $repoBranch
    Assert-ExitCodeIsZero
    popd
}

popd
