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
dotnet tool update --global docfx

Write-Host 'Updating dotnet-serve tool...' -ForegroundColor Black -BackgroundColor Green
dotnet tool update --global dotnet-serve

if (-not $NoFetch) {
    $external_docs = @{
        # use either commit, or branch name to use its latest commit
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
    }


    # In case an external contributor wants to import their forked repository, specify the custom Git Url below and uncomment the following lines and the additional parameters for the script execution:
    # $contributor_git_url = "https://github.com/ContributorUserName/"

    # $forks_to_import = @(
    #     "uno.extensions"
    # )

    Write-Host 'Importing external repositories...' -ForegroundColor Black -BackgroundColor Green
    ./import_external_docs $external_docs # $contributor_git_url $forks_to_import
}
else {
    Write-Host '-nofetch option specified. Skipping the import of external repositories.' -ForegroundColor Black -BackgroundColor DarkRed
}

# Generate the documentation
docfx

# Serve it
dotnet-serve --open-browser:_site/articles/intro.html
