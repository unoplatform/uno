# The Uno docs website and docfx

Uno Platform's docs website uses [docfx](https://dotnet.github.io/docfx/) to convert Markdown files in the [articles folder](https://github.com/unoplatform/uno/tree/master/doc/articles) into [html files](https://platform.uno/docs/articles/intro.html).

## Adding to the table of contents

Normally when you add a new doc file, you also add it to [articles/toc.yml](https://github.com/unoplatform/uno/blob/master/doc/articles/toc.yml). This allows it to show up in the left sidebar Table of Contents on the docs website.

### TOC checker script

The [`check_toc` script](https://github.com/unoplatform/uno/blob/master/doc/articles/check_toc.ps1) checks for dead links in the TOC, as well as Markdown files in the 'articles' folder that are not part of the TOC. At the moment it's not part of the CI, but contributors can run it locally and fix any bad or missing links.

<!-- TODO: ## Anchor links -->

## Building docs website locally with docfx

Sometimes you may want to run docfx locally to validate that changes you've made look good in html. To do so you'll first need to generate the 'implemented views' documentation.

To run docfx locally and check the resulting html:

1. Open the `Uno.UI-Tools.slnf` solution filter in the `src` folder with Visual Studio. 
2. Edit the properties of the `Uno.UwpSyncGenerator` project. Under the 'Debug' tab, set Application arguments to "doc".
3. Set `Uno.UwpSyncGenerator` as startup project and run it. It may fail to generate the full implemented views content; if so, it should still nonetheless generate stubs so that docfx can run successfully.
4. Navigate to `%USERPROFILE%\.nuget\packages\docfx.console`. If you don't see the docfx package in your NuGet cache, go back to ``Uno.UI-Tools.slnf`, right-click on the solution and choose 'Restore NuGet Packages.'
5. Open the latest docfx version and open the `tools` folder.
6. Open a Powershell window in the `tools` folder.
7. Run the following command: `.\docfx "C:\src\Uno.UI\doc\docfx.json" -o C:\src\Uno.UI\docs-local-dist`, replacing `C:\src\Uno.UI` with your local path to the Uno.UI repository.
8. When docfx runs successfully, it will create the html output at `C:\src\Uno.UI\docs-local-dist\_site`, which you can now view or mount on a local server.