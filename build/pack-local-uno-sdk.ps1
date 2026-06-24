#!/usr/bin/env pwsh
# Packs the in-repo Uno.Sdk as Uno.Sdk.Private at a fixed sentinel version into
# the local PackageCache feed, so the in-repo SamplesApp head can consume it via global.json.
$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/.."
$version  = '255.255.255-dev'
$feed     = Join-Path $repoRoot 'src/PackageCache'

# Uno.Sdk has GeneratePackageOnBuild=true and a CoreCompile-time version-replacement
# dance, so it must be *built* (not `dotnet pack`, which skips the compile -> NU5026).
# --disable-build-servers keeps this one-shot pack from spawning a persistent
# VBCSCompiler / MSBuild node that would keep .dotnet/* DLLs locked.
dotnet build "$repoRoot/src/Uno.Sdk/Uno.Sdk.csproj" `
  -c Release `
  --disable-build-servers `
  -p:PackageVersion=$version `
  -p:PackageOutputPath=$feed

# Belt-and-suspenders: shut down any build server this (or a prior) step left running.
# On CI the next step restores a cached .dotnet *over* the live SDK with 7-Zip; a lingering
# server holding .dotnet/shared/**/*.dll open makes that overwrite fail ("Access is denied").
dotnet build-server shutdown | Out-Null

Write-Host "Packed Uno.Sdk.Private $version -> $feed"
