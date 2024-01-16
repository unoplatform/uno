---
uid: Uno.UI.CommonIssues.vscode
---

# Issues related to VS Code

## Known limitations for VS Code support

- C# Debugging is not supported when running in a remote Linux Container, Code Spaces or GitPod.
- C# Hot Reload for WebAssembly only supports modifying method bodies. Any other modification is rejected by the compiler.
- C# Hot Reload for Skia supports modifying method bodies, adding properties, adding methods, adding classes. A more accurate list is provided here in Microsoft's documentation.

## Troubleshooting Uno Platform VS Code issues

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](external/uno.check/doc/using-uno-check.md) should be your first step.

The Uno Platform extension provides multiple output windows to troubleshoot its activities:

- **Uno Platform**, which indicates general messages about the extension
- **Uno Platform - Debugger**, which provides activity messages about the debugger feature
- **Uno Platform - Hot Reload**, which provides activity messages about the Hot Reload feature
- **Uno Platform - XAML**, which provides activity messages about the XAML Code Completion feature

If the extension is not behaving properly, try using the `Developer: Reload Window` (or `Ctrl+R`) command in the palette.
