---
uid: Uno.UI.CommonIssues.vscode
---

# Issues related to VS Code

## Known limitations for VS Code support

- C# Debugging is not supported when running in a remote Linux Container, Code Spaces, or GitPod.
- Calls to `InitializeComponent()` may show intellisense errors until the Windows head has been built once.

## Troubleshooting Uno Platform VS Code issues

IFor assistance configuring or running Android or iOS emulators, see the [Android & iOS emulator troubleshooting guide](xref:Uno.UI.CommonIssues.MobileDebugging).

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](external/uno.check/doc/using-uno-check.md) should be your first step.

The Uno Platform extension provides multiple output windows to troubleshoot its activities:

- **Uno Platform**, which indicates general messages about the extension
- **Uno Platform - Debugger**, which provides activity messages about the debugger feature
- **Uno Platform - Hot Reload**, which provides activity messages about the Hot Reload feature
- **Uno Platform - XAML**, which provides activity messages about the XAML Code Completion feature

![Extension Outputs](Assets/quick-start/vs-code-extension-outputs.png)

They are also accessible using the status bar Uno logo: hover your mouse pointer over the logo, and the extension status will be shown along with links to the related outputs.

![Extension Status](Assets/quick-start/vs-code-extension-status.png)

If the extension is not behaving properly, try using the `Developer: Reload Window` (or `Ctrl+R`) command in the palette.

## Reporting issues

You can report issues directly from VS Code by either:

- using the Uno logo status bar (see screenshot above); or
- pressing `F1` and selecting `Uno Platform: Report Issue...`

The form is already pre-filled with some useful information to help diagnose issues.
Follow the comments to complete the report.
