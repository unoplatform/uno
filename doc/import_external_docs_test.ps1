<#
    import_external_docs_test.ps1 [-NoFetch]

    -NoFetch   : Skip the import of external repositories. Useful for testing or authoring.
                 The default is to import the external repositories from the main branch.
#>

param(
    [Alias('NF')]
    [switch] $NoFetch
)

Write-Host 'Updating docfx tool...' -ForegroundColor Black -BackgroundColor Green
# Intentionally pinned to match the CI docs build toolchain (build/Uno.UI.Build.csproj)
# so local validation reproduces doc generation behavior consistently.
dotnet tool update --global docfx --version 2.73.2

Write-Host 'Updating dotnet-serve tool...' -ForegroundColor Black -BackgroundColor Green
dotnet tool update --global dotnet-serve

if (-not $NoFetch) {
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

    Write-Host 'Importing external repositories...' -ForegroundColor Black -BackgroundColor Green
    ./import_external_docs $external_docs
} else {
    Write-Host '-nofetch option specified. Skipping the import of external repositories.' -ForegroundColor Black -BackgroundColor DarkRed
}

# Generate the documentation
docfx

# Serve it
dotnet-serve --open-browser:_site/articles/intro.html
