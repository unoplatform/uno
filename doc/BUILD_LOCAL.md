# Building Documentation Locally

This guide explains how to build the complete Uno Platform documentation locally, including all external repositories.

## Prerequisites

- Node.js and npm
- PowerShell Core (pwsh)
- Git

## Quick Start

To build the documentation with all external repos included:

```bash
cd doc

# 1. Import external documentation
pwsh import_external_docs.ps1

# 2. Build the documentation
npm run build

# 3. Start local server (with live reload)
npm start
```

The documentation will be available at http://localhost:3000

## What Gets Imported

The `import_external_docs.ps1` script imports documentation from these repositories:

- **uno.extensions** - Extensions for navigation, MVUX, HTTP, etc.
- **uno.themes** - Material and Cupertino design themes
- **uno.toolkit.ui** - Additional UI controls and helpers
- **uno.check** - Environment setup and validation tool
- **uno.wasm.bootstrap** - WebAssembly bootstrapper
- **uno.xamlmerge.task** - XAML merging build task
- **uno.resizetizer** - Image and asset resizing tool
- **uno.uitest** - UI testing framework
- **uno.chefs** - Sample applications and recipes
- **workshops** - Workshop and tutorial content
- **uno.samples** - Sample applications
- **figma-docs** - Figma plugin documentation
- **hd-docs** - Hot Design documentation

## External Documentation Structure

External documentation is cloned into:
- `articles/external/{repo-name}/` - For most repos
- `articles/studio/Hot Design/` - For Hot Design docs

## Build Commands

### Full build with external docs
```bash
pwsh import_external_docs.ps1
npm run build
```

### Quick rebuild (uses existing external docs)
```bash
npm run build
```

### Clean build
```bash
npm run clean
pwsh import_external_docs.ps1
npm run build
```

### Build and serve with live reload
```bash
npm start
# or
npm run debug
```

## How Import Script Works

The `import_external_docs.ps1` script:

1. Clones each external repository (if not already present)
2. Checks out specific commit hashes to ensure consistency
3. Pulls latest changes if repositories already exist
4. Uses sparse checkout for efficiency

The commit hashes are defined in the script and updated periodically to point to stable versions.

## Updating External Docs

To pull the latest changes for external docs:

```bash
cd doc
pwsh import_external_docs.ps1
```

This will:
- Skip cloning if repos already exist
- Fetch and checkout the specified commits
- Update to the latest versions defined in the script

## Troubleshooting

### External docs not appearing

If external documentation isn't showing up:

1. Verify the import ran successfully:
   ```bash
   ls -la articles/external/
   ```

2. Check for errors during import:
   ```bash
   pwsh import_external_docs.ps1 2>&1 | tee import.log
   ```

3. Clean and reimport:
   ```bash
   rm -rf articles/external/
   pwsh import_external_docs.ps1
   npm run build
   ```

### Build errors after import

If the build fails after importing:

1. Check DocFX output for specific errors
2. Verify all TOC files are present
3. Try a clean build:
   ```bash
   npm run clean
   npm run build
   ```

### Memory issues during build

For large documentation builds:

```bash
export NODE_OPTIONS="--max-old-space-size=4096"
npm run build
```

## CI/CD Integration

In CI pipelines, always run the import script before building:

```yaml
- name: Import external docs
  run: pwsh doc/import_external_docs.ps1

- name: Build documentation
  run: npm run build
  working-directory: doc
```

## Development Workflow

For documentation development:

1. **First time setup:**
   ```bash
   cd doc
   npm install
   pwsh import_external_docs.ps1
   ```

2. **Daily development:**
   ```bash
   npm start  # Starts with live reload
   ```
   
3. **When external docs change:**
   ```bash
   pwsh import_external_docs.ps1
   # Server will auto-reload if running
   ```

## Notes

- The import script is safe to run multiple times
- Existing repositories are updated, not deleted
- External docs use detached HEAD state to ensure consistency
- Changes to external docs should be made in their source repositories
- The server at http://localhost:3000 has live reload enabled
- BrowserSync UI is available at http://localhost:3001
