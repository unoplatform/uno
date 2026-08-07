#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Fails the build when native UI rendering artifacts reappear in the tree.

.DESCRIPTION
	7.0 drops the native UI renderers, but `master` still carries them, so every
	master -> feature/* sync can re-offer a deleted file. A modify/delete conflict
	resolved in master's favour silently restores it (see the sync workflow at
	.github/workflows/master-sync.yml).

	Three checks, none of which needs a hand-maintained list of deleted paths:

	  1. No project under src/ references a .csproj that does not exist. A
	     resurrected head is orphaned by definition, so its references dangle.
	  2. No Uno.UI-and-higher project declares UnoRuntimeIdentifier=WebAssembly.
	     The retained non-UI platform assemblies still legitimately do.
	  3. No src/Uno.UI source file branches on a native-only symbol that its two
	     remaining heads (Skia, Reference) never define.
#>

[CmdletBinding()]
param(
	[string]$RepositoryRoot = (Resolve-Path "$PSScriptRoot/../../..")
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path $RepositoryRoot).Path.TrimEnd('\', '/')
$srcRoot = Join-Path $repoRoot "src"
if (-not (Test-Path $srcRoot)) {
	throw "Scan root '$srcRoot' not found. This script needs a full checkout, not a sparse one."
}

# Non-UI platform APIs are still built for the browser: the Skia heads consume them.
$webAssemblyRuntimeAllowList = @(
	"src/Uno.Foundation",
	"src/Uno.Foundation.Runtime.WebAssembly",
	"src/Uno.UI.Dispatching",
	"src/Uno.UWP"
)

# Vendored Mono sources that reference the Mono class libraries, absent by design.
$danglingReferenceAllowList = @(
	"src/SourceGenerators/System.Xaml/System.Xaml-net_4_x.csproj"
)

# Symbols no src/Uno.UI head defines. Native symbols still live in the WebView interop
# files linked into the Skia Android/AppleUIKit heads, so they are deliberately not listed.
$deadSymbolPattern = '^\s*#\s*(if|elif)\b.*(__WASM__|XAMARIN)'
$liveSkiaHostPattern = 'ANDROID_SKIA|UIKIT_SKIA|WASM_SKIA'

$failures = New-Object System.Collections.Generic.List[string]

function Get-RelativePath([string]$path) {
	return $path.Substring($repoRoot.Length + 1).Replace('\', '/')
}

# --- 1. Dangling ProjectReference -------------------------------------------------
foreach ($proj in Get-ChildItem $srcRoot -Recurse -Include *.csproj -File) {
	$rel = Get-RelativePath $proj.FullName
	if ($danglingReferenceAllowList -contains $rel) { continue }

	$content = (Get-Content $proj.FullName -Raw) ?? ""
	foreach ($m in [regex]::Matches($content, '<ProjectReference\s+Include\s*=\s*"([^"]+)"')) {
		$include = $m.Groups[1].Value
		# Globs and MSBuild expressions are resolved at build time, not here.
		if ($include -match '[\*\$%]') { continue }

		$target = Join-Path $proj.Directory.FullName $include.Replace('\', [IO.Path]::DirectorySeparatorChar)
		if (-not (Test-Path $target)) {
			$failures.Add("$rel references a project that does not exist: $include")
		}
	}
}

# --- 2. Native WASM-DOM runtime identifier in the UI layer ------------------------
foreach ($proj in Get-ChildItem $srcRoot -Recurse -Include *.csproj -File) {
	if ((((Get-Content $proj.FullName -Raw) ?? "")) -notmatch '<UnoRuntimeIdentifier>\s*WebAssembly\s*</UnoRuntimeIdentifier>') { continue }

	$rel = Get-RelativePath $proj.FullName
	if ($webAssemblyRuntimeAllowList | Where-Object { $rel.StartsWith("$_/") }) { continue }

	$failures.Add("$rel declares UnoRuntimeIdentifier=WebAssembly; the native WASM-DOM UI heads were removed in 7.0.")
}

# --- 3. Dead native #if in src/Uno.UI --------------------------------------------
foreach ($file in Get-ChildItem (Join-Path $srcRoot "Uno.UI") -Recurse -Include *.cs -File) {
	$lineNumber = 0
	foreach ($line in Get-Content $file.FullName) {
		$lineNumber++
		# Case-sensitive: XAMARIN the symbol, not "xamarin" in a comment or URL.
		if ($line -cmatch $deadSymbolPattern -and $line -cnotmatch $liveSkiaHostPattern) {
			$failures.Add("$(Get-RelativePath $file.FullName):$lineNumber branches on a symbol no Uno.UI head defines: $($line.Trim())")
		}
	}
}

# --- Report ----------------------------------------------------------------------
if ($failures.Count -eq 0) {
	Write-Host "No native UI rendering regressions found."
	exit 0
}

foreach ($failure in $failures) {
	if ($env:TF_BUILD) {
		Write-Host "##vso[task.logissue type=error]$failure"
	}
	elseif ($env:GITHUB_ACTIONS) {
		Write-Host "::error::$failure"
	}
	else {
		Write-Host "ERROR: $failure"
	}
}

Write-Host ""
Write-Host "$($failures.Count) native UI rendering regression(s) found."
Write-Host "These artifacts were removed for 7.0. If a master sync restored them, keep the deletion."
exit 1
