---
uid: Uno.Contributing.ExternalDocsImport
---

# External Docs Import (Uno Repos & Forks)

The Uno documentation repository provides two PowerShell scripts to import and test external documentation sources from various Uno Platform repositories.

## Overview

The documentation generation process can pull content from multiple external repositories (such as `uno.wasm.bootstrap`, `uno.themes`, `uno.toolkit.ui`, etc.) into the local `articles/external` directory. This allows contributors to test documentation changes from their forks before submitting pull requests.

## `import_external_docs.ps1`

This script imports various external repositories into the local documentation structure. By default, it uses fixed commits/branches configured in the script, but you can override them using parameters.

### Repository Configuration (ref and optional dest)

Inside `import_external_docs.ps1`, each external repository is declared in the `$external_docs` map using the form:

```powershell
"repo.name" = @{ ref = "<commit-or-branch>"; dest = "optional/sub/folder" }
```

- `ref` (required): Either a full commit SHA or a branch name (latest commit of that branch will be fetched).
- `dest` (optional): A relative sub-folder under `articles/` where the repository's docs will be placed.
  - If `dest` is omitted, content is placed in `articles/external/<repo.name>`.
  - You can use nested paths and spaces (e.g., `studio/Hot Design`).

Example (taken from the script):

```powershell
"hd-docs" = @{ ref = "aa81f09e08fe0357e2aed15a7eab59d6c27728d9"; dest = "studio/Hot Design" }
```

Resulting import path: `articles/studio/Hot Design/`

> [!NOTE]
> The current parameters (`-branches`, `-contributor_git_url`, `-forks_to_import`) only override the commit/branch (`ref`). They do not change `dest`. To alter destination paths, update the `$external_docs` configuration directly.

### Parameters

#### [`-branches <Hashtable>`](#tab/branches)

Overrides the default commit/branch for individual repositories.
Accepts repository name as key and branch/commit as value

**Example:**

```powershell
@{ "uno.wasm.bootstrap" = "main" }
```

#### [`-contributor_git_url <string>`](#tab/contributor-git-url)

Specifies the GitHub URL of a contributor's fork to import repositories from instead of the main Uno Platform repository.

**Rules:**

- Must be a valid HTTP/HTTPS URL
- Should end with a trailing slash (`/`) to safely append repository names, otherwise the script will add it automatically

**Example:**

`https://github.com/ContributorUserName/`

#### [`-forks_to_import <string[]>`](#tab/forks-to-import)

Optional.
Array of repository names to import from the contributor fork.

**Rules:**

- Repository names must match the configured names in `$external_docs`
- Comparison is case-insensitive

Example array:

```powershell
@(
  "uno.wasm.bootstrap",
  "uno.themes"
  )
```

### Usage Examples

#### [Default Import](#tab/default)

Import all repositories from Uno Platform using configured commits/branches:

```powershell
./import_external_docs.ps1
```

#### [Fork with Default Branch](#tab/fork-default)

Import specific repository from a contributor fork using the default branch/commit configured in `$external_docs`:

```powershell
./import_external_docs.ps1
  -contributor_git_url "https://github.com/ContributorUserName/"
  -forks_to_import @("uno.wasm.bootstrap")
```

#### [Fork with Pull Request Branch](#tab/fork-custom)

Import specific repository from a contributor fork with a custom branch override:

```powershell
./import_external_docs.ps1
  -contributor_git_url "https://github.com/ContributorUserName/"
  -branches @{
    "uno.wasm.bootstrap" = "main",
    "uno.extensions" = "feature-branch"
    }
  -forks_to_import @(
    "uno.wasm.bootstrap",
    "uno.extensions"
    )
```

#### [Branch Override](#tab/branch-override)

Override branch for multiple repositories while fetching from Uno Platform:

```powershell
./import_external_docs.ps1 -branches @{
   "uno.wasm.bootstrap" = "main";
   "uno.themes" = "feature-branch"
   }
```

---

## Important Notes

> [!NOTE]
> If a contributor fork is specified, the script will attempt to import the specified repositories from that fork instead of the main Uno Platform repository.
> You need to make sure the forked repository name matches the expected names (e.g., `uno.wasm.bootstrap`).
> The repository name comparison is case-insensitive.
> [!TIP]
> The `-branches` parameter is independent of `-forks_to_import`:
>
> - Use `-contributor_git_url` together with `-forks_to_import` to import from a fork using the default configured branch/commit
> - Combine both parameters with `-branches` to import from a fork with a custom branch/commit override

## `import_external_docs_contrib.ps1`

This script provides a complete workflow for testing documentation changes locally. It updates the necessary tools, imports external documentation sources, generates the documentation, and starts a local server.

### Script Parameters

- **`-NoFetch`** (Alias: `-NF`): Skips cloning/updating the external repositories. Useful for local testing if the sources are already present and you only want to regenerate the documentation.

### Script Usage Examples

```powershell
# Full workflow with import of external sources
./import_external_docs_contrib.ps1

# Skip re-import (only regenerate documentation and start server)
./import_external_docs_contrib.ps1 -NoFetch
```

### What the Script Does

1. Updates `docfx` and `dotnet-serve` tools
2. Imports external documentation sources (unless `-NoFetch` is specified)
3. Generates the documentation using DocFX
4. Starts a local HTTP server

The generated documentation will be available locally at `http://localhost:5000/articles/intro.html`.

## Workflow for Contributors

When working on documentation changes in a forked repository:

1. **Make your changes** in your fork of the external repository (e.g., `uno.wasm.bootstrap`)
2. **Push your branch** to your fork
3. **Import your fork** using the import script:

   ```powershell
   ./import_external_docs.ps1 -branches @{ "uno.wasm.bootstrap" = "your-branch-name" } -contributor_git_url "https://github.com/YourUserName/" -forks_to_import @("uno.wasm.bootstrap")
   ```

4. **Test locally** using the test script:

   ```powershell
   ./import_external_docs_contrib.ps1 -NoFetch
   ```

5. **Review** your changes at `http://localhost:5000`
6. **Submit pull request** once verified

## Troubleshooting

### Fork Repository Name Mismatch

If you receive an error about repository names not being configured in `external_docs`, ensure your forked repository name exactly matches the expected name (case-insensitive). For example:

- ✅ Correct: `uno.wasm.bootstrap`
- ❌ Incorrect: `uno-wasm-bootstrap` or `Uno.Wasm.Bootstrap-fork`

### Branch Not Found

If the script cannot find the specified branch, verify:

1. The branch exists in your fork
2. The branch name is spelled correctly
3. You've pushed the branch to your remote fork

## See Also

- [Building the Documentation Locally](xref:Uno.Contributing.DocFx#building-docs-website-locally-with-docfx)
- [Contributing to Uno Platform](xref:Uno.Contributing.Intro)
