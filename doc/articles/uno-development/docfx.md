---
uid: Uno.Contributing.DocFx
---

<!-- markdownlint-disable MD001 -->

# The Uno docs website and DocFX

Uno Platform's docs website uses [DocFX](https://dotnet.github.io/docfx/) to convert Markdown files in the [articles folder](https://github.com/unoplatform/uno/tree/master/doc/articles) into [html files](xref:Uno.Documentation.Intro).

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

`WinUI.Namespace`

# [Uno Platform](#tab/tabid-2)

`Uno.Namespace`

---
```

Html output:

# [WinUI](#tab/tabid-1)

`WinUI.Namespace`

# [Uno Platform](#tab/tabid-2)

`Uno.Namespace`

---

### TOC checker script

The [`check_toc` script](https://github.com/unoplatform/uno/blob/master/doc/articles/check_toc.ps1) checks for dead links in the TOC, as well as Markdown files in the 'articles' folder that are not part of the TOC. At the moment it's not part of the CI, but contributors can run it locally and fix any bad or missing links.

<!-- TODO: ## Anchor links -->

## Building docs website locally with DocFX

Sometimes you may want to run DocFX locally to validate that changes you've made look good in html. To do so you'll first need to generate the 'implemented views' documentation.

### Run DocFX locally

To run DocFX locally and check the resulting html:

1. Open the `Uno.UI-Tools.slnf` solution filter in the `src` folder with Visual Studio.
2. Edit the properties of the `Uno.UwpSyncGenerator` project. Under the 'Debug' tab, set Application arguments to "doc".
3. Set `Uno.UwpSyncGenerator` as startup project and run it. It may fail to generate the full implemented views content; if so, it should still nonetheless generate stubs so that DocFX can run successfully.
4. Navigate to `%USERPROFILE%\.nuget\packages\docfx.console`. If you don't see the DocFX package in your NuGet cache, go back to ``Uno.UI-Tools.slnf`, right-click on the solution and choose 'Restore NuGet Packages.'
5. Open the latest DocFX version and open the `tools` folder.
6. Open a Powershell window in the `tools` folder.
7. Run the following command: `.\docfx "C:\src\Uno.UI\doc\docfx.json" -o C:\src\Uno.UI\docs-local-dist`, replacing `C:\src\Uno.UI` with your local path to the Uno.UI repository.
8. When DocFX runs successfully, it will create the html output at `C:\src\Uno.UI\docs-local-dist\_site`, which you can now view or mount on a local server.

### Use a local server

You can use `dotnet-serve` as a simple command-line HTTP server for example.

1. Install `dotnet-serve` using the following command: `dotnet tool install --global dotnet-serve`. For more info about its usage and options,
[please refer to the documentation](https://github.com/natemcmaster/dotnet-serve).
2. Using the command prompt, navigate to `C:\src\Uno.UI\docs-local-dist\_site` (replacing `C:\src\Uno.UI` with your local path to the Uno.UI repository) and run the following command `dotnet serve -o -S`. This will start a simple server with HTTPS and open the browser directly.

## Run the documentation generation performance test

If needed, you can also run a script that will give you a performance summary for the documentation generation.

To run the script on Windows:

1. Make sure `crosstargeting_override.props` is not defining UnoTargetFrameworkOverride
2. Open a Developer Command Prompt for Visual Studio (2019 or 2022)
3. Go to the uno\build folder (not the uno\src\build folder)
4. Run the `run-doc-generation.cmd` script; make sure to follow the instructions

### Importing external documentation sources

The Uno documentation repository provides two PowerShell scripts to import and test external documentation sources:

#### `import_external_docs.ps1`

This script imports various external repositories (e.g., uno.wasm.bootstrap, uno.themes, etc.) into the local `articles/external` directory. By default, fixed commits are used, but you can override them using parameters.

**Parameters:**

- `-branches <Hashtable>`: Overrides the default commit/branch for individual repositories. Example: `@{ "uno.wasm.bootstrap" = "main" }`
- `-contributor_git_url <string>`: Optional. Git URL of a contributor fork, e.g., `https://github.com/ContributorUserName/`.
- `-forks_to_import <string[]>`: Optional. List of repository names to import from the contributor fork.

**Example usage:**

```powershell
# Import all default repositories
./import_external_docs.ps1

# Import specific repositories from a fork and branch
./import_external_docs.ps1 -branches @{ "uno.wasm.bootstrap" = "main" } -contributor_git_url "https://github.com/ContributorUserName/" -forks_to_import @( "uno.wasm.bootstrap" )
```

#### `import_external_docs_test.ps1`

This script updates the tools (`docfx`, `dotnet-serve`), imports the external documentation sources (unless `-NoFetch` is specified), generates the documentation, and starts a local server.

**Parameters:**

- `-NoFetch` (Alias: `-NF`): Skips cloning/updating the external repositories. Useful for local testing if the sources are already present.

**Example usage:**

```powershell
# With import of external sources
./import_external_docs_test.ps1

# Without re-import (only generation & server)
./import_external_docs_test.ps1 -NoFetch
```

The generated documentation will be available locally at `http://localhost:5000/articles/intro.html`.

---
