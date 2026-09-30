# build-app.ps1 — Build the WinUI (WinAppSDK) SamplesApp head.
#
# Usage:
#   pwsh -File build-app.ps1 [-Mode Build|Package] [-Strict] [-Thumbprint <sha1>] [-BinLog <path>]
#
#   Build   (default) compile only — what `winapp run` needs. No MSIX, no signing.
#   Package compile + MSIX + signing — only for the packaging/CI-parity path.
#
# Handles the two environment details that otherwise break the build:
#   * the .NET SDK pinned in build/ci/net11/_global.json (the repo-root global.json sets
#     allowPrerelease:false and would select a .NET 10 SDK, failing with NETSDK1045);
#   * BuildGraphics3DGLForWindows, passed globally so the add-in restores its Windows TFM
#     (otherwise CS0012 / NETSDK1005 from the Skia-only build).

param(
	[ValidateSet('Build', 'Package')][string]$Mode = 'Build',

	# Drop UnoFastDevBuild so analyzers and code-style run as they do on CI
	[switch]$Strict,

	# Signing certificate thumbprint — Package mode only (see setup-cert.ps1)
	[string]$Thumbprint = '',

	[string]$BinLog = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
Push-Location $repoRoot

$project = "src/SamplesApp/SamplesApp/SamplesApp.csproj"

# Use the SDK version this repo pins for CI, wherever it is installed side-by-side.
$pinnedGlobalJson = Join-Path $repoRoot "build\ci\net11\_global.json"
$pinnedVersion = (Get-Content $pinnedGlobalJson -Raw | ConvertFrom-Json).sdk.version
if (-not $pinnedVersion) {
	throw "build\ci\net11\_global.json has no sdk.version to pin."
}

$overridePath = Join-Path $repoRoot "src\crosstargeting_override.props"
if (-not (Test-Path $overridePath)) {
	throw "src\crosstargeting_override.props is missing. Create it with UnoTargetFrameworkOverride = the windows TFM (see SKILL.md Phase 1)."
}
# Parsed as XML so a commented-out override does not count; the TFM must match the pinned SDK's major version.
$requiredTfmPrefix = "net$(([version]($pinnedVersion -replace '-.*$', '')).Major).0-windows"
$override = ([xml](Get-Content $overridePath -Raw)).SelectNodes('//*[local-name()="UnoTargetFrameworkOverride"]') |
	Select-Object -Last 1
if (-not $override -or -not $override.InnerText.Trim().StartsWith($requiredTfmPrefix)) {
	throw "src\crosstargeting_override.props must set UnoTargetFrameworkOverride to $($requiredTfmPrefix)10.0.19041.0 (found '$($override.InnerText)')."
}

$sdkRoot = @("$env:ProgramFiles\dotnet", "$env:LOCALAPPDATA\Microsoft\dotnet") |
	Where-Object { Test-Path (Join-Path $_ "sdk\$pinnedVersion") } |
	Select-Object -First 1
if (-not $sdkRoot) {
	throw "SDK $pinnedVersion not found. Install it side-by-side: dotnet-install.ps1 -Version $pinnedVersion -InstallDir `"$env:LOCALAPPDATA\Microsoft\dotnet`" -NoPath"
}
$env:DOTNET_ROOT = $sdkRoot
$env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $sdkRoot
$env:PATH = "$sdkRoot;$env:PATH"
Write-Host "Using .NET SDK $pinnedVersion from $sdkRoot"

$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
	-prerelease -all -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
	throw "MSBuild.exe not found. Install Visual Studio with the MSBuild component."
}

$msbuildArgs = @(
	$project, '-restore', '-m', '-v:m', '-nologo',
	'-p:Configuration=Release', '-p:Platform=x64', '-p:RuntimeIdentifier=win-x64',
	'-p:BuildGraphics3DGLForWindows=true')
# Passed either way: a global property also overrides a value set in crosstargeting_override.props.
$msbuildArgs += if ($Strict) { '-p:UnoFastDevBuild=false' } else { '-p:UnoFastDevBuild=true' }
if ($BinLog) {
	$msbuildArgs += "-bl:$BinLog"
}
if ($Mode -eq 'Build') {
	$msbuildArgs += '-t:Build'
}
else {
	if (-not $Thumbprint) {
		throw "Package mode needs -Thumbprint (run setup-cert.ps1 and read ~/.uno-dev-cert-thumbprint)."
	}
	$msbuildArgs += @('-t:Publish', '-p:GenerateAppxPackageOnBuild=true', "-p:PackageCertificateThumbprint=$Thumbprint")
}

# Builds of one checkout share its global.json (and the head's obj folders), so run them one at a time;
# otherwise a second run would back up the already-pinned global.json and restore that.
$mutex = [System.Threading.Mutex]::new($false, "Local\UnoWinUIBuildApp_$($repoRoot -replace '[\\/:]', '_')")
try {
	if (-not $mutex.WaitOne(0)) {
		Write-Host "Another build-app.ps1 is running in this checkout; waiting for it to finish..."
		[void]$mutex.WaitOne()
	}
}
catch [System.Threading.AbandonedMutexException] {
	# The previous holder exited without releasing the lock; it is ours now.
}

# CI copies the pinned global.json over the repo-root one; do the same, and always restore it.
$backup = Join-Path ([System.IO.Path]::GetTempPath()) "uno-global-json-$PID.bak"
$sw = [Diagnostics.Stopwatch]::StartNew()
try {
	Copy-Item (Join-Path $repoRoot "global.json") $backup -Force
	Copy-Item $pinnedGlobalJson (Join-Path $repoRoot "global.json") -Force
	& $msbuild @msbuildArgs
	$exitCode = $LASTEXITCODE
}
finally {
	if (Test-Path $backup) {
		Copy-Item $backup (Join-Path $repoRoot "global.json") -Force
		Remove-Item $backup -Force -ErrorAction SilentlyContinue
	}
	$mutex.ReleaseMutex()
	$mutex.Dispose()
	Pop-Location
}

Write-Host "$Mode finished in $($sw.Elapsed) (exit $exitCode)"
if ($exitCode -ne 0) {
	throw "Build failed with exit code $exitCode."
}
