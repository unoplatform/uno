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
}

./import_external_docs $external_docs

docfx

dotnet-serve --open-browser:_site/articles/intro.html
