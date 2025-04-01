---
uid: Uno.Contributing.DocFx
---

<!-- markdownlint-disable MD001 -->

# The Uno documentation website and DocFX

Uno Platform's docs website uses [DocFX](https://dotnet.github.io/docfx/) to convert Markdown (.md) files in the [articles folder](https://github.com/unoplatform/uno/tree/master/doc/articles) into [HTML files](xref:Uno.Documentation.Intro).

## Linking to the Table of Contents

Normally when you add a new markdown file, you also add it to [articles/toc.yml](../../toc.yml). This allows it to show up in the left sidebar TOC on the docs website.

### Checking Links in the Table of Contents

To ensure that your file is correctly linked and nothing is missing, you can use the [Toc Checker](xref:Uno.Contributing.check-toc.Overview). This helps identify unreferenced files and invalid links automatically.

## DocFX-flavored Markdown

DocFX supports extended Markdown syntaxes that are treated specially when converting to html.

### Formatted blockquotes

You can use [specially-styled blockquotes](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html#note-warningtipimportant), to call special attention to particular information.

The following note types are supported, including an example for each one:

```markdown
> [!NOTE]
> Information the user should notice even if skimming.
```

> [!NOTE]
> Information the user should notice even if skimming.

```markdown
> [!TIP]
> Optional information to help a user be more successful.
```

> [!TIP]
> Optional information to help a user be more successful.

```markdown
> [!IMPORTANT]
> Essential information required for user success.
```

> [!IMPORTANT]
> Essential information required for user success.

```markdown
> [!CAUTION]
> Negative potential consequences of an action.
```

> [!CAUTION]
> Negative potential consequences of an action.

```markdown
> [!WARNING]
> Dangerous certain consequences of an action.
```

> [!WARNING]
> Dangerous certain consequences of an action.

### Tabs

DocFX can generate tabs. Make sure to follow the [syntax specification](https://dotnet.github.io/docfx/docs/markdown.html#tabs) precisely.

#### Example

Markdown:

```md
# [WinUI](#tab/tabid-1)

`Microsoft.UI.Xaml.FrameworkElement`

# [UWP](#tab/tabid-2)

`Windows.UI.Xaml.FrameworkElement`

---
```

Html output:
<!-- markdownlint-disable MD051 -->
# [WinUI](#tab/tabid-1)

`Microsoft.UI.Xaml.FrameworkElement`

# [UWP](#tab/tabid-2)

`Windows.UI.Xaml.FrameworkElement`
<!-- markdownlint-enable MD51 -->
---

> [!NOTE]
> Use `---` in the Markdown sample is Important, to not include more Content in the tabbed area than actually wanted, but will not be rendered in the served documentation.
> It is possible to use `***` alternatively for the same task.

<!-- TODO: Add ### Dependant Tabs -->

## Building docs website locally with DocFX

Sometimes, you may want to run DocFX locally to ensure that your changes render correctly in HTML. To do this, first generate the *implemented views* documentation. If you've added any new documentation files, make sure to [validate the contents of the TOC](#checking-links-in-the-toc) to minimize warnings and avoid potential build errors.

### Run DocFX locally

To run DocFX locally and check the resulting html:

1. Open the `Uno.UI-Tools.slnf` solution filter in the `src` folder with Visual Studio.
2. Edit the properties of the `Uno.UwpSyncGenerator` project. Under the 'Debug' tab, set Application arguments to "doc".
3. Set `Uno.UwpSyncGenerator` as startup project and run it. It may fail to generate the full implemented views content; if so, it should still nonetheless generate stubs so that DocFX can run successfully.
4. Open a Terminal at the Root Directory of your locally cloned Uno Repository.
5. Install docfx globally: `dotnet tool install -g docfx`
6. Run the following command: `docfx build doc/docfx.json` and attach any nested folder name you want by adding `-o your-nested-output-path`, default: `_site`
7. When DocFX builds successfully, it will create the html output at `uno-clone-repo\doc\[your-nested-output-path\]_site`, which you can serve by one of the following options:
   a. Execute the command `docfx serve doc/docfx.json` in your terminal.
   b. Use a [local server](#use-a-local-server).

### Use a local server

You can use `dotnet-serve` as a simple command-line HTTP server for example.

1. Install `dotnet-serve` using the following command: `dotnet tool install --global dotnet-serve`. For more info about its usage and options,[please refer to the documentation](https://github.com/natemcmaster/dotnet-serve).
2. Using the command prompt, navigate to `C:\src\Uno.UI\docs-local-dist\_site` (replacing `C:\src\Uno.UI` with your local path to the Uno.UI repository) and run the following command `dotnet serve -o -S`. This will start a simple server with HTTPS and open the browser directly.

## Run the documentation generation performance test

If needed, you can also run a script that will give you a performance summary for the documentation generation.

To run the script on Windows:

1. Make sure `crosstargeting_override.props` is not defining UnoTargetFrameworkOverride
2. Open a Developer Command Prompt for Visual Studio (2019 or 2022)
3. Go to the uno\build folder (not the uno\src\build folder)
4. Run the `run-doc-generation.cmd` script; make sure to follow the instructions

## Import Uno Extensions and Tools docs

[!INCLUDE [Import external docs](../../external/uno.extensions/doc/README.md)]
