Set-PSDebug -Trace 1

$external_docs =
@(
    @("https://github.com/unoplatform/uno.wasm.bootstrap", "uno.wasm.bootstrap", "2ca1f3f426d59c56cf8c4c553470858c53361dd9"),
    @("https://github.com/unoplatform/uno.themes", "uno.themes", "2b9d747cc068a38acd75f544db3fa49b0694a035"),
    @("https://github.com/unoplatform/uno.toolkit.ui", "uno.toolkit.ui", "5a395c9b69b176c722dbce0eb975f2e9284a9c80"),
    @("https://github.com/unoplatform/uno.check", "uno.check", "479b01d10240e2c506912c4af46197c1497c9971"),
    @("https://github.com/unoplatform/uno.xamlmerge.task", "uno.xamlmerge.task", "a6d2efa69e24e8280c38300b5c1b7a8f2033f9f9"),
    @("https://github.com/unoplatform/figma-docs", "figma-docs", "4904915548a31725d4ba81683f6e300a0a84c9b5"),
    @("https://github.com/unoplatform/uno.extensions", "uno.extensions", "87c21e1d94e05a1adb7712d601aa47ee4da38f3c")
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
for($i = 0; $i -lt $external_docs.Length; $i++)
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
