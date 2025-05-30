echo 'Updating docfx tool...'
dotnet tool update --global docfx

echo 'Updating dotnet-serve tool...'
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
    "uno.chefs"          = "main"
}

# In case an external contributor wants to import his forked repository, specify the custom Git Url below and uncomment the following lines and the additional parameters for the script execution:
# $contributor_git_url = "https://github.com/ContributorUserName/"

# $forks_to_import = @(
#     "Uno"
# )

./import_external_docs $external_docs # $contributor_git_url $forks_to_import

docfx

dotnet-serve --open-browser:_site/articles/intro.html
