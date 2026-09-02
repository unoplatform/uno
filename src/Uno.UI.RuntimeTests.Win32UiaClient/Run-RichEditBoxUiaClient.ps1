param(
	[string] $AppPath = "",
	[switch] $NativeInstalled
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$appProject = Join-Path $repositoryRoot "src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj"
$projectPath = Join-Path $PSScriptRoot "RichEditBoxUiaClient.csproj"

function Get-MSBuildProperty([string] $Project, [string] $Property, [string[]] $AdditionalArguments = @())
{
	$output = & dotnet msbuild $Project -nologo "-getProperty:$Property" @AdditionalArguments
	if ($LASTEXITCODE -ne 0)
	{
		throw "Failed to evaluate MSBuild property '$Property' for '$Project'."
	}

	return ($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1).Trim()
}

if (-not $NativeInstalled -and [string]::IsNullOrWhiteSpace($AppPath))
{
	$appFramework = Get-MSBuildProperty $appProject "NetCurrent"
	$AppPath = Get-MSBuildProperty $appProject "TargetPath" @(
		"-property:Configuration=Release",
		"-property:TargetFramework=$appFramework"
	)
}

if (-not $NativeInstalled -and -not (Test-Path $AppPath))
{
	throw "SamplesApp was not found at '$AppPath'. Build SamplesApp.Skia.Generic in Release first."
}

$appProcess = $null

try
{
	$clientFramework = Get-MSBuildProperty $projectPath "NetCurrentWinAppSDK"
	dotnet build $projectPath -c Release -f $clientFramework --nologo --verbosity quiet
	if ($LASTEXITCODE -ne 0)
	{
		throw "The external UIA client failed to compile."
	}
	$clientPath = Get-MSBuildProperty $projectPath "TargetPath" @(
		"-property:Configuration=Release",
		"-property:TargetFramework=$clientFramework"
	)

	$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
	if ($NativeInstalled)
	{
		$startInfo.FileName = "unosamplesapp.exe"
		$startInfo.WorkingDirectory = $repositoryRoot
		$startInfo.Arguments = "sample=RichEditBox/RichEditBox_UIAutomation"
	}
	else
	{
		$startInfo.FileName = "dotnet"
		$startInfo.WorkingDirectory = Split-Path $AppPath
		$startInfo.Arguments = "`"$AppPath`" sample=RichEditBox/RichEditBox_UIAutomation"
	}
	$startInfo.UseShellExecute = $false
	$startInfo.RedirectStandardOutput = $true
	$startInfo.RedirectStandardError = $true
	$appProcess = [System.Diagnostics.Process]::Start($startInfo)
	if ($null -eq $appProcess)
	{
		throw "SamplesApp failed to start."
	}
	$appProcess.BeginOutputReadLine()
	$appProcess.BeginErrorReadLine()

	if ($NativeInstalled)
	{
		Start-Sleep -Seconds 5
		dotnet $clientPath 0 native
	}
	else
	{
		dotnet $clientPath $appProcess.Id
	}
	if ($LASTEXITCODE -ne 0)
	{
		throw "External RichEditBox UIA validation failed."
	}
}
finally
{
	if ($null -ne $appProcess -and -not $appProcess.HasExited)
	{
		Stop-Process -Id $appProcess.Id
		$appProcess.WaitForExit()
	}

	Remove-Item (Join-Path $PSScriptRoot "obj") -Recurse -Force -ErrorAction SilentlyContinue
}
