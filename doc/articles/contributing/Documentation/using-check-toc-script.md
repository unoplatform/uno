---
uid: Uno.Contributing.check-toc
---

# TOC Checker Overview

The [`check_toc.ps1`](../check_toc.ps1) script helps maintain the structure and integrity of your documentation by:

- Detecting **broken links** in [toc.yml](../toc.yml)
- Identifying **Markdown files** recursive to the `articles` folder that are **not referenced** in the TOC
- Providing a YAML-formatted suggestion (`toc_additions.yml.tmp`) for missing entries

> [!NOTE]
> This script is currently not part of the CI pipeline. Contributors can run it manually to ensure link consistency before submitting documentation changes.

The script checks for dead links in the **Table of Contents (TOC)**, which you can explore on the left side of the Uno Documentation Website, as well as Markdown files in the [`articles`](../../../articles/) folder of the Uno Platform [`unoplatform/uno`](https://github.com/unoplatform/uno) repository that are not part of the TOC.

## How-To: Using the TOC Checker Script

This guide is for contributors working with the Uno documentation who want to verify the structure and consistency of the DocFX table of contents (TOC). The `check_toc.ps1` script identifies broken or missing links and helps ensure all Markdown articles are properly referenced in `toc.yml`.

### Prerequisites

- Any Uno Platform supported IDE (Visual Studio 2022, VS Code, Rider, etc.)
- PowerShell 5.1+ or PowerShell Core 7+
- Local clone of the Uno documentation repository
- Write access to the `doc/articles` directory to generate temporary output files

---

### Step 1 – Open PowerShell

Open a PowerShell terminal and navigate to the root of your Uno clone.

```powershell
cd c:\Users\YourUsername\source\repos\unoplatform\uno
```

### Step 2 – Navigate to the `articles` Directory

The script must be executed from within the folder containing the `toc.yml` file.

```powershell
cd doc\articles
```

> [!IMPORTANT]
> Relative paths are resolved from this directory. Executing from elsewhere will yield incorrect results.

### Step 3 – Run the Script

Execute the script using the following command:

```powershell
& .\check_toc.ps1
```

### Step 4 – Review the Console Output

The script will display:

- **Broken links** - TOC entries referencing files that don't exist
- **Unlinked files** - Markdown files in the project not referenced in the TOC
- **Status messages** - Confirmation when no issues are found

Example output:

```text
No bad links found in toc.yml
.md files not linked in toc.yml: .\check-toc-utilities\Guide.md
Missing links added to toc_additions.yml.tmp
```

### Step 5 – Review the Generated File

A new file `toc_additions.yml.tmp` will be created in the `doc/articles` directory containing suggested entries for unlinked files.

> [!NOTE]
> Visual Studio 2022 does not show the generated `.tmp` file by default in Solution Explorer.
> To open it manually:
>
> 1. Use **File > Open > File** from the menu
> 2. Navigate to `doc\articles\`
> 3. Select the file from the file picker
>
> ![Opening toc_additions.yml.tmp in Visual Studio](./assets/check-toc-find-toc-additions-file.gif)

### Step 6 – Add Missing Entries to TOC

Open `toc_additions.yml.tmp` to see the suggested entries in YAML format. The script extracts the first `#` header from each unlinked file and generates basic TOC entries:

```yaml
# UNLINKED .MD FILES: Add to toc.yml in appropriate category
    - name: How-To: Using the TOC Checker Script
      href: check-toc-utilities/Guide.md
```

## Adding Entries to toc.yml

Copy the relevant entries from `toc_additions.yml.tmp` and add them to [`toc.yml`](../toc.yml) in the **appropriate** category, maintaining the existing structure and indentation.

> [!TIP]
> The script generates basic `href:` file path references. If a file has a `uid:` in its YAML front matter, you may want to manually convert the `href:` to an `xref:` reference for location independence. For detailed information about TOC linking best practices, see the [Linking to the TOC](xref:Uno.Contributing.DocFx#linking-to-the-toc) documentation.

## Troubleshooting

### No Output Generated

If nothing is reported and no `toc_additions.yml.tmp` file is created:

- Verify the script was executed from the `doc/articles` directory
- Check that you have write permissions in the directory
- Ensure `toc.yml` exists in the current directory

### File Permission Issues

If you encounter "Access Denied" or similar errors:

- Close any applications that might have `toc_additions.yml.tmp` open
- Check your user permissions for the `doc/articles` directory
- Try running PowerShell as Administrator (Windows) or with appropriate permissions

### Script Execution Policy Error

If you see "cannot be loaded because running scripts is disabled":

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Generated File Not Visible in Visual Studio

> [!NOTE]
> Visual Studio 2022 does not show `.tmp` files by default.
> Use **File > Open > File** to manually navigate to and open `toc_additions.yml.tmp` in the `doc\articles` directory.

## Related Pages

- [TOC Checker Script Overview](xref:Uno.Contributing.check-toc) - Comprehensive overview of the script
- [The Uno docs website and DocFX](xref:Uno.Contributing.DocFx) - General documentation guidelines

---

[!INCLUDE [getting-help](includes/getting-help.md)]
