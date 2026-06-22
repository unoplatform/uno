#!/usr/bin/env pwsh
# Packs the in-repo Uno.Sdk as Uno.Sdk.Private at a fixed sentinel version into
# the local PackageCache feed, so the in-repo SamplesApp head can consume it via global.json.
$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/.."
$version  = '255.255.255-dev'
$feed     = Join-Path $repoRoot 'src/PackageCache'

# Uno.Sdk has GeneratePackageOnBuild=true and a CoreCompile-time version-replacement
# dance, so it must be *built* (not `dotnet pack`, which skips the compile -> NU5026).
dotnet build "$repoRoot/src/Uno.Sdk/Uno.Sdk.csproj" `
  -c Release `
  -p:PackageVersion=$version `
  -p:PackageOutputPath=$feed

Write-Host "Packed Uno.Sdk.Private $version -> $feed"
