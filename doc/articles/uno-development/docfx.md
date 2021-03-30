# The Uno docs website and DocFX

Uno Platform's docs website uses [DocFX](https://dotnet.github.io/docfx/) to convert Markdown files in the [articles folder](https://github.com/unoplatform/uno/tree/master/doc/articles) into [html files](https://platform.uno/docs/articles/intro.html).

## Adding to the table of contents

Normally when you add a new doc file, you also add it to [articles/toc.yml](https://github.com/unoplatform/uno/blob/master/doc/articles/toc.yml). This allows it to show up in the left sidebar Table of Contents on the docs website.

## DocFX-flavored Markdown

DocFX supports extended Markdown syntaxes that are treated specially when converting to html.

### Formatted blockquotes

You can declare a [specially-styled blockquote](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html#note-warningtipimportant) like so:

```md
> [!NOTE]
> This is a Note, showing how to declare notes.
```

This is how it looks when converted to .html:

> [!NOTE]
> This is a Note, showing how to declare notes.

Use pre-formatted blockquotes when you want to call special attention to particular information.

The following note types are supported:

```md
> [!NOTE]
> ...

> [!TIP]
> ...

> [!WARNING]
> ...

> [!IMPORTANT]
> ...

> [!CAUTION]
> ...

```

### Tabs

DocFX can generate tabs. Make sure to follow the [syntax specification](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html#tabbed-content) precisely.

#### Example

Markdown:

```md
# [WinUI](#tab/tabid-1)

`Microsoft.UI.Xaml.FrameworkElement`

# [UWP](#tab/tabid-2)

`Windows.UI.Xaml.FrameworkElement`

***
```

Html output:

# [WinUI](#tab/tabid-1)

`Microsoft.UI.Xaml.FrameworkElement`

# [UWP](#tab/tabid-2)

`Windows.UI.Xaml.FrameworkElement`

***

### TOC checker script

The [`check_toc` script](https://github.com/unoplatform/uno/blob/master/doc/articles/check_toc.ps1) checks for dead links in the TOC, as well as Markdown files in the 'articles' folder that are not part of the TOC. At the moment it's not part of the CI, but contributors can run it locally and fix any bad or missing links.

<!-- TODO: ## Anchor links -->

## Building docs website locally with DocFX

Sometimes you may want to run DocFX locally to validate that changes you've made look good in html. To do so you'll first need to generate the 'implemented views' documentation.

To run DocFX locally and check the resulting html:

1. Open the `Uno.UI-Tools.slnf` solution filter in the `src` folder with Visual Studio. 
2. Edit the properties of the `Uno.UwpSyncGenerator` project. Under the 'Debug' tab, set Application arguments to "doc".
3. Set `Uno.UwpSyncGenerator` as startup project and run it. It may fail to generate the full implemented views content; if so, it should still nonetheless generate stubs so that DocFX can run successfully.
4. Navigate to `%USERPROFILE%\.nuget\packages\docfx.console`. If you don't see the DocFX package in your NuGet cache, go back to ``Uno.UI-Tools.slnf`, right-click on the solution and choose 'Restore NuGet Packages.'
5. Open the latest DocFX version and open the `tools` folder.
6. Open a Powershell window in the `tools` folder.
7. Run the following command: `.\docfx "C:\src\Uno.UI\doc\docfx.json" -o C:\src\Uno.UI\docs-local-dist`, replacing `C:\src\Uno.UI` with your local path to the Uno.UI repository.
8. When DocFX runs successfully, it will create the html output at `C:\src\Uno.UI\docs-local-dist\_site`, which you can now view or mount on a local server.