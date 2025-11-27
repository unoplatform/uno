<#
    import_external_docs_canary.ps1
    
    Imports external documentation repositories from their default branches for canary builds.
    This script is used by the CI/CD pipeline to build the canary documentation site.
#>

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> branch (default branch for canary)
$external_docs = @{
    "uno.wasm.bootstrap" = "main"
    "uno.themes"         = "master"
    "uno.toolkit.ui"     = "main"
    "uno.check"          = "main"
    "uno.xamlmerge.task" = "main"
    "figma-docs"         = "main"
    "uno.resizetizer"    = "main"
    "uno.uitest"         = "master"
    "uno.extensions"     = "main"
    "workshops"          = "master"
    "uno.samples"        = "master"
    "uno.chefs"          = "main"
    "hd-docs"            = "main"
}

Write-Host 'Importing external repositories for CANARY build...' -ForegroundColor Black -BackgroundColor Green
./import_external_docs $external_docs

Set-PSDebug -Off
