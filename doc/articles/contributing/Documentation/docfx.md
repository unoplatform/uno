---
uid: Uno.Contributing.docfx
---

<!-- markdownlint-disable MD001 -->

# The Uno documentation website and DocFX

*[TOC]: Table of Contents
*[HTML]: Hypertext Markup Language
*[docs]: documentation
*[doc file]: markdown file

Uno Platform's docs website uses [DocFX](https://dotnet.github.io/docfx/) to convert Markdown (.md) files in the [articles folder](https://github.com/unoplatform/uno/tree/master/doc/articles) into [HTML files](xref:Uno.Documentation.Intro).

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

## Linking to the TOC

When you add a new doc file, you should also add it to [articles/toc.yml](../toc.yml). This allows it to show up in the left sidebar TOC on the docs website.

### Example: Adding a file to the TOC

1. **Add a `uid:` to your Markdown file's front matter header**:

   ```markdown
   ---
   uid: Uno.MyFeature.Guide
   ---

   # My Feature Guide

   Content here...
   ```

2. **Reference it in `toc.yml` using `xref:`**:

   ```yaml
   - name: My Feature Guide
     href: xref:Uno.MyFeature.Guide
   ```

> [!NOTE]
> **Prefer using `xref:` references in the TOC** rather than direct file paths with `href:`.
> This provides:
>
> - **Location independence:** - Files can be moved or renamed without breaking TOC links
> - **Consistent resolution:** - DocFX resolves references based on the `uid:` in each file's YAML front matter header
> - **Better maintainability:** - Changes to file structure don't require TOC updates
> [!CAUTION]
> Files intended for `[!INCLUDE ...]` usage must not have a `uid:` in their YAML front matter. These are reusable content snippets included in multiple documents. This is a known DocFX limitation - see [issue #9947](https://github.com/dotnet/docfx/issues/9947).

## Nested Documentation Topics with toc.yml

For complex topics with multiple related files, create a **dedicated folder with its own `toc.yml`** file. This provides better organization and allows for a landing page when users expand the topic.

**To do this, you can follow these steps:**

1. **Create a folder structure** for your topic:

   ```plaintext
   articles/
   └── my-feature/
       ├── toc.yml          # Nested TOC for this topic
       ├── overview.md      # Landing page (should have a uid)
       ├── guide.md         # Related content
       └── examples.md      # More content
   ```

2. **In the nested folder's `toc.yml`**, use `xref:` for files with `uid:`:

   ```yaml
   - name: Overview
     href: xref:Uno.MyFeature.Overview
   - name: Guide
     href: xref:Uno.MyFeature.Guide
   - name: Examples
     href: xref:Uno.MyFeature.Examples
   ```

3. **In the main `toc.yml`**, reference the folder's TOC with **both `href:` and `topicHref:`**:

   ```yaml
   - name: My Feature
     href: my-feature/toc.yml                    # Links to the nested TOC
     topicHref: xref:Uno.MyFeature.Overview      # Landing page displayed when topic is expanded
   ```

> [!TIP]
> The `topicHref` property defines what content is displayed when a user **clicks on an expandable topic** in the documentation sidebar. Always provide a landing page for nested topics that gives users an overview of the information covered. This allows users to quickly determine if the topic contains what they're looking for.
>
> **Key benefits:**
>
> - **Better user experience** - Immediate overview content when expanding a topic
> - **Clear navigation** - Users see the landing page before diving into sub-topics
> - **Consistent hierarchy** - Main topic shows its overview, sub-items show specific content
> - **Easier maintenance** - Clear separation and independence of the main TOC and nested topics

### Checking Links in the TOC with the TOC Checker

To ensure that your file is correctly linked and nothing is missing, you can use the [TOC Checker](xref:Uno.Contributing.check-toc). This helps identify unreferenced files and invalid links automatically, supporting both `xref:` and `href:` link validation.

## Tabs

DocFX can generate tabs. Make sure to follow the [syntax specification](https://dotnet.github.io/docfx/docs/markdown.html#tabs) for general guidiance on this.

While the [docfx documentation guidiance for Tabs](https://dotnet.github.io/docfx/docs/markdown.html?tabs=linux%2Cdotnet#tabs) is showing Tab markdown with just one `#`, in the Uno Documentation the TOC on the right side of the the produced documentation page, will not render properly, if we use single `#` for Tabbed Content.

So you need to make sure to use the appropriate heading levels as per the document structure.

### Example

Markdown:

```md
# My Feature

## [WinUI](#tab/tabid-1)

`WinUI.Namespace`

## [Uno Platform](#tab/tabid-2)

`Uno.Namespace`

---
```

Html output:
<!-- markdownlint-disable MD051 MD025 -->

# My Feature

## [WinUI](#tab/tabid-1)

`WinUI.Namespace`

## [Uno Platform](#tab/tabid-2)

`Uno.Namespace`

---

> [!NOTE]
> Use `---` in the Markdown sample is Important, to not include more Content in the tabbed area than actually wanted, but will not be rendered in the served documentation.
> [!TIP]
> It is possible to use `***` alternatively for the same task.
> [!NOTE]
> When using tabs, ensure that each tab header is unique within the same document to avoid Markdown validation and rendering issues.

Find information about how to link specific Headers inside of a markdown file in the [Anchor Links in Documentation](xref:Uno.Contributing.Documentation.Anchor-links) article.

## Building docs website locally with DocFX

Sometimes, you may want to run DocFX locally to ensure that your changes render correctly in HTML. To do this, first generate the *implemented views* documentation. If you've added any new documentation files, make sure to [validate the contents of the TOC](#checking-links-in-the-toc) to minimize warnings and avoid potential build errors.

### Run DocFX locally

To run DocFX locally and check the resulting html:

1. Open the `Uno.UI-Tools.slnf` solution filter in the `src` folder with Visual Studio.
2. Edit the properties of the `Uno.UwpSyncGenerator` project. Under the 'Debug' tab, set Application arguments to "doc".
3. Set `Uno.UwpSyncGenerator` as startup project and run it. It may fail to generate the full implemented views content; if so, it should still nonetheless generate stubs so that DocFX can run successfully.
<!-- 4. Navigate to `%USERPROFILE%\.nuget\packages\docfx.console`. If you don't see the DocFX package in your NuGet cache, go back to ``Uno.UI-Tools.slnf`, right-click on the solution and choose 'Restore NuGet Packages.' UNDONE: DocFx.console is depreciated and not longer available, see more Information to this here: https://github.com/dotnet/docfx/issues/9100
5. Open the latest DocFX version and open the `tools` folder. UNDONE: tools\DocFx.exe is now nested in .dotnet folder and available throught `dotnet tool`
6. Open a Powershell window in the `tools` folder. UNDONE: Terminal in the IDE is enough -->
4. Open a Terminal at the Root Directory of your locally cloned Uno Repository.
5. Install docfx globally: `dotnet tool install -g docfx`
6. Run the following command: `docfx build doc/docfx.json` and attach any nested foldername you want by adding `-o your-nested-output-path`, default: `_site`
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

[!INCLUDE [advises-to-import-external-docs](./external/uno.extensions/doc/README.md)]
