# dotnet-root.ps1 — Dot-sourced by run-tests.ps1 and run-tests-msix.ps1.
#
# The app is framework-dependent and net11 previews are usually installed side-by-side, so
# DOTNET_ROOT must point at an install that has the exact runtime the app was built against.

function Set-DotnetRootForApp([string]$appDir) {
	$runtimeConfig = Get-ChildItem $appDir -Filter "*.runtimeconfig.json" -ErrorAction SilentlyContinue | Select-Object -First 1
	if (-not $runtimeConfig) {
		Write-Warning "No *.runtimeconfig.json in $appDir; DOTNET_ROOT left unchanged."
		return
	}

	$options = (Get-Content $runtimeConfig.FullName -Raw | ConvertFrom-Json).runtimeOptions
	$framework = @($options.framework) + @($options.frameworks) |
		Where-Object { $_ -and $_.name -eq 'Microsoft.NETCore.App' } |
		Select-Object -First 1
	if (-not $framework.version) {
		return
	}
	$version = $framework.version

	# An inherited DOTNET_ROOT is only kept when it actually contains the runtime.
	$candidates = @($env:DOTNET_ROOT, "$env:ProgramFiles\dotnet", "$env:LOCALAPPDATA\Microsoft\dotnet") |
		Where-Object { $_ } | Select-Object -Unique
	foreach ($root in $candidates) {
		if (Test-Path (Join-Path $root "shared\Microsoft.NETCore.App\$version")) {
			if ($env:DOTNET_ROOT -ne $root) {
				$env:DOTNET_ROOT = $root
				Write-Host "DOTNET_ROOT set to $root (runtime $version)"
			}
			return
		}
	}
	Write-Warning "No installed .NET runtime $version found (searched $($candidates -join ', ')); the app may fail to start."
}
