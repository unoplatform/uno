---
uid: Uno.Contributing.check-toc.Overview
---
# Toc Checker Overview

The [`check_toc.ps1`](../check_toc.ps1) script helps maintain the structure and integrity of your documentation by:

- Detecting **broken links** in [toc.yml](../toc.yml)
- Identifying **Markdown files** recursive to `articles` folder that are **not referenced** in the TOC
- Providing a YAML-formatted suggestion (`toc_additions.yml.tmp`) for missing entries

> [!NOTE]
> This script is currently not part of the CI pipeline. Contributors can run it manually to ensure link consistency.

## 🛠️ Usage

1. Open a **PowerShell terminal** at the **root** of your locally cloned Uno repository.
2. Navigate to the [`doc/articles`](../../articles/) directory:

   ```ps1
   cd doc/articles
   ```

   > [!IMPORTANT]
   > The script must be executed from within `doc/articles` to correctly resolve relative paths in TOC entries.

3. Run the script:

   ```ps1
   & .\check_toc.ps1
   ```

4. Open the generated file `toc_additions.yml.tmp` to see which files should be added to `toc.yml`.

> [!TIP]
> Run with `-Verbose` to see detailed output and trace the script’s progress.
> Consider using an external Power-Shell Terminal in this case, since some IDE like vs code might limit the output lines you can review after executing with `-Verbose` flag.

## 🔍 What It Does

The script performs the following Check-up Tasks:

- **Parses `toc.yml`** and extracts Markdown (`.md`) and `xref:` links.
- **Scans the current directory recursively** to collect all Markdown files.
- **Compares files in the directory vs. TOC entries**, excluding known or auto-generated ones (e.g., `implemented-views.md`, `roadmap.md`, etc.)
- **Identifies files** that:
  - Are referenced in the TOC but don't exist (broken links)
  - Exist in the project but are missing from the TOC (unlinked files)

Utility functions are dynamically imported from [`Import-TocCheckerUtils.ps1`](./Import-TocCheckerUtils.ps1), providing modular support for file parsing, Markdown processing, and header detection.

## 📄 Related Pages

- [The Uno docs website and DocFX](xref:Uno.Contributing.DocFx)
- [Utility Functions for `check_toc`](xref:Uno.Contributing.check-toc.Utilities)

## 🌐 Further Reading

- [!INCLUDE [Clean Architecture Principles Inline](../includes/clean-architecture-principles-inline.md)]
