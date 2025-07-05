# Documentation Uno

This folder contains source code for the generation of uno's documentation

> [!IMPORTANT]
> It's very important that you read the deploy section before committing anything to the repo.

## Running a local environment

### Install dependencies

Download and install DocFx on your computer.

#### macOS

```bash
brew install docfx
```

#### Windows

```bash
choco install docfx
```

#### Node

Use a node version manager or the version of node specified in the `.nvmrc` file nvm or nvs

```bash
nvs use
```

or

```bash
nvm use
```

Then install the dependencies

```bash
npm install
```

## Generated Implemented views

The process of generating implemented views is documented on this page. [Building docs website locally with docfx](xref:Uno.Contributing.docfx#building-docs-website-locally-with-docfx).
As stated in the documentation, it will probably fail, but it will create stub files and let docfx build without errors.
By default, the build swallows docfx errors (it prints them in the console), that is for simplicity since you don't need
the implemented views. To test docfx and break on error run the `npm run strict` command.

## Deploy

docfx will use the content of the `styles` folder when building. When working locally, source-maps are generated to help
debugging the site; the javascript and css are not minified for the same reason. It's very important that the
build command is ran just before committing your work; this will minify the code, clean up the `styles` and `_site`
folders and build the docfx according to the `docfx.json`. The CI only runs the docfx command, it will not regenerate
the `styles` folder.

## Commands

### Start

With browsersync and gulp watch, any changes in the sass, js and Docfx templates should be rebuilt automatically.
This command starts the project with the debug flag. This prevents the js from being minified and generates source-maps
(easier debugging). It will concatenate all the js into one `docfx.js` file.

```bash
npm start
```

### Build

Will build the docfx documentation according to the `docfx.json` file, will minify and concatenate all javascript
everything in the `docfx.js` file (except`main.js`) and will compile and minify the sass. This command needs to be run
before committing any changes to the repos.

```bash
npm run build
```

### Prod

This command is similar to start, but it will minify the js and the sass and won't generate any source-maps.

```bash
npm run prod
```

### Strict

The reference pages are generate by the CI and are not there locally. This causes errors when building docfx. You can
generate stub pages (see in the **Generate Implemented Views** section). Since generating those is often unnecessary, it's
faster to generate them only if they are needed. When running the command strict, it is the same as running the Prod
command but the errors won't be ignored.

```bash
npm run strict
```

### Clean

This command will erase the content of the `styles` and `_site/styles` folders.

```bash
npm run clean
```

## Basic structure

The templating files are in the folder `layout` and `partial`. The javascript and scss files associated to a component
are in the `component` directory. The javascript functions and utilities are in the `service` folder. The shared constant
are in the `constant.js` file. The render order of the component is important; the functions are called in the `render.js`
file. Every js file is concatenated into the `docfx.js` file in the `styles` folder and every scss file is concatenated into
the `main.css`.

The `docfx.vendor.*` files in the vendor folder are there to freeze the dependencies. To update them, delete the files
and copy the newly generated files from the `_site/style` folder.

Every file in the `styles` folder is automatically generated and should not be modified manually.

### Spell-checking the docs

Spell-checking for the docs is done as part of a GitHub Action.

If you'd like to perform the same check locally, you can use the vs code extension `Code Spell Checker` and configure it to use the [cSpell.json](../build/cSpell.json) file in the directory `build` of the Uno Repository, or use the npm provided version and run:

* `npm install -g cspell@latest` to install the cSpell CLI
* `cspell --config ./cSpell.json "doc/**/*.md" --no-progress` to check all the markdown files in the `doc` folder.

### Markdown linting the docs

Markdown linting for the docs is done as part of a GitHub Action.

If you'd like to perform the same check locally, you can use the vs code extension `markdownlint` or use npm with running:

* `npm install -g markdownlint-cli@latest` to install the markdownlint CLI
* `markdownlint "doc/**/*.md"` to lint all the markdown files in the `doc` folder.

You can also install the [markdownlint Visual Studio Code Extension](https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint) to easily check markdown linting and style while writing documentation.

## Notes

The local environment is usually located on port `3000` unless another process is already using it.

You have to remove the `docs` fragment from the WordPress menu to reach your local documentation server.

For detailed instructions on how to run docfx locally, please refer to the [docfx Local Setup Guide](xref:Uno.Contributing.docfx).
