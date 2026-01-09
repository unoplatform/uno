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

Sometimes you may want to run DocFX locally to validate that changes you've made look good in html.

### Prerequisites

Before building the documentation locally, ensure you have the following installed:

#### Install DocFX

# [macOS](#tab/tabid-macos)

```bash
brew install docfx
```

# [Windows](#tab/tabid-windows)

```bash
choco install docfx
```

> [!NOTE]
> On Windows, run the `choco install docfx` command from an elevated PowerShell prompt (Run as Administrator) to avoid permission errors.

---

#### Install Node.js

Use a Node.js version manager (nvm or nvs) with the version specified in the `.nvmrc` file in the `doc` folder.

```bash
# Using nvs
nvs use

# Or using nvm
nvm use
```

Then install the npm dependencies:

```bash
cd doc
npm install
```

### Generate implemented views (optional)

"Implemented views" are documentation pages that are generated from the Uno source code to describe how specific APIs are implemented across platforms. They are consumed by DocFX as part of the full docs build, but most day-to-day documentation changes (guides, how-tos, etc.) do not require regenerating them.

You typically only need to generate implemented views when you are working on the tooling that produces them, or when you want to validate a full local DocFX build that includes these generated pages. For most contributors, this step can be safely skipped.

If you do need to generate them:

1. Open the `Uno.UI-Tools.slnf` solution filter in the `src` folder with Visual Studio.
2. Edit the properties of the `Uno.UwpSyncGenerator` project. Under the 'Debug' tab, set Application arguments to "doc".
3. Set `Uno.UwpSyncGenerator` as startup project and run it. It may fail to generate the full implemented views content; if so, it should still nonetheless generate stubs so that DocFX can run successfully.

> [!NOTE]
> By default, the build does not fail on DocFX errors (it prints them in the console). This is for simplicity since you don't need the implemented views for most documentation work. To test DocFX and break on error, run the `npm run strict` command.

### Build and serve the documentation locally
#### Using npm commands (Recommended)

The easiest way to build and serve the documentation locally is using npm scripts:

```bash
cd doc

# Start development server with live reload
npm start
```

This command will:

- Start a local development server with BrowserSync
- Watch for changes in Sass, JavaScript, and DocFX templates
- Automatically rebuild and reload the browser when changes are detected
- Generate source maps for easier debugging
- Serve the documentation at `http://localhost:3000` (or the next available port)

#### Other npm commands

```bash
# Build for production (minified, no source maps)
# Run this before committing changes
npm run build

# Build and serve with production settings
npm run prod

# Build with strict error checking (fails on DocFX errors)
npm run strict

# Clean generated files
npm run clean
```

> [!IMPORTANT]
> Always run `npm run build` before committing changes to ensure the production assets are properly generated and minified.

#### Using DocFX directly

If you prefer to run DocFX directly without npm:

1. Navigate to the `doc` folder.
2. Run DocFX with the configuration file:

```bash
docfx docfx.json
```

3. The generated HTML will be in the `_site` folder.

### Serve the built documentation

After building with DocFX directly, you can serve the documentation using a local HTTP server.

#### Using dotnet-serve

`dotnet-serve` is a simple command-line HTTP server:

1. Install `dotnet-serve`:

```bash
dotnet tool install --global dotnet-serve
```

2. Navigate to the `_site` folder and start the server:

```bash
cd _site
dotnet serve -o -S
```

This will start a server with HTTPS and open the browser automatically.

For more information about `dotnet-serve` usage and options, [please refer to the documentation](https://github.com/natemcmaster/dotnet-serve).

## Testing Algolia DocSearch locally

The documentation website uses Algolia DocSearch for search functionality. The search is configured in `doc/templates/uno/partials/scripts.tmpl.partial`.

### DocSearch configuration

The DocSearch implementation is already integrated into the documentation templates and will work automatically when you serve the documentation locally using `npm start` or after building with `npm run build`.

The search configuration includes:

- **App ID**: `PHB9D8WS99`
- **Index Name**: `platform`
- **Container**: `#docsearch` (located in the sidebar)

### Testing search locally

When running the documentation locally:

1. Start the development server:

```bash
cd doc
npm start
```

2. Open your browser to the local server URL (typically `http://localhost:3000`).
3. The search box should appear in the left sidebar.
4. Type a search query to test the search functionality.

> [!NOTE]
> The search uses the production Algolia index, so search results will reflect the content currently deployed to the live documentation site, not your local changes. To see your local content in search results, your changes need to be deployed to production and the Algolia index needs to be updated.

### How DocSearch works locally

The DocSearch initialization is handled by JavaScript in the `scripts.tmpl.partial` file:

- A `MutationObserver` watches for the `#docsearch` element to be added to the DOM
- When detected, it initializes the Algolia DocSearch widget
- The search connects to the Algolia API to fetch results from the production index

### Debugging DocSearch

If the search isn't working as expected:

1. Open your browser's Developer Tools Console
2. Look for DocSearch initialization messages (e.g., "DocSearch initialized")
3. Check for any errors related to Algolia or DocSearch
4. Verify the `#docsearch` element exists in the DOM (it's in the sidebar's `.sidefilter` div)

## Run the documentation generation performance test

If needed, you can also run a script that will give you a performance summary for the documentation generation.

To run the script on Windows:

1. Make sure `crosstargeting_override.props` is not defining UnoTargetFrameworkOverride
2. Open a Developer Command Prompt for Visual Studio (2019 or 2022)
3. Go to the uno\build folder (not the uno\src\build folder)
4. Run the `run-doc-generation.cmd` script; make sure to follow the instructions
