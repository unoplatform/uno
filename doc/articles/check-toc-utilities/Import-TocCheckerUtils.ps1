function Import-TocCheckerUtils {
    [CmdletBinding()]
    param (
        [string]$UtilsFolder = $PSScriptRoot,
        [string[]]$ExcludeFiles = @()
    )

    # Resolve script location if UtilsFolder is not set or invalid
    if (-not $UtilsFolder -or -not (Test-Path $UtilsFolder)) {
        $UtilsFolder = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
    }

    if (-not (Test-Path $UtilsFolder)) {
        throw "The specified UtilsFolder path '$UtilsFolder' does not exist."
    }

    Write-Host "Collecting TOC Checker Utils from: $UtilsFolder"

    # Get the path of the current script to exclude it
    $mySelf = $MyInvocation.MyCommand.Path

    # Collect all .ps1 files, excluding this script, excluded files, and test scripts
    $ps1Files = Get-ChildItem -Path $UtilsFolder -Filter *.ps1 | Where-Object {
        $_.FullName -ne $mySelf -and
        ($_.Name -notin $ExcludeFiles) -and
        ($_.Name -notmatch '\.Test\.ps1$')
    }

    foreach ($file in $ps1Files) {
        $absolutePath = $file.FullName
        try {
            . $absolutePath
            Write-Host "✅ Dot-sourced: $($file.Name)"
        }
        catch {
            Write-Warning "❌ Failed to dot-source: $($file.Name)"
            Write-Warning $_
        }
    }
}