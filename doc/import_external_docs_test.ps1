Write-Output 'Updating docfx tool...'
dotnet tool update --global docfx

Write-Output 'Updating dotnet-serve tool...'
dotnet tool update --global dotnet-serve

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
}

# In case you want to import a specific branch wich is not already committed in the uno-origin Repository, specify custom Git URLs for specific repositories and uncomment the following lines:
# $contributor_git_urls = @{
#     "uno.themes" = "https://github.com/contributor-fork/uno.themes"
# }

# dot import the main script
./import_external_docs.ps1

# call the function to import external docs
Invoke-ImportExternalDocs -branches $external_docs -custom_git_urls $contributor_git_urls

docfx build -Path $PSScriptRoot.Replace('import_external_docs_test.ps1', 'docfx.json')

dotnet-serve --open-browser:$PSScriptRoot.Replace('import_external_docs_test.ps1', '_site/articles/intro.html')
