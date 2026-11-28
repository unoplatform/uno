---
uid: Uno.Contributing.DebuggingTemplates
---

# Debugging Uno Platform Solution templates

The Uno Platform templates (the `dotnet new` templates and the new-project wizards' options) live in the [unoplatform/uno.templates](https://github.com/unoplatform/uno.templates) repository, not in this one.

## dotnet new templates

To make modifications to those templates and test the result, follow [Building and testing locally](https://github.com/unoplatform/uno.templates#building-and-testing-locally) in the uno.templates README. In short:

- Build the templates package. The README's script builds it as version `255.255.255.255` and installs it.
- If you build the package yourself instead of running the script, install it with `dotnet new install <path-to>/Uno.Templates.255.255.255.255.nupkg`.
- In another temporary folder, create a new application using the updated templates with `dotnet new unoapp -o MyTestUnoApp01`.

Ensure that the application runs properly by building it and running it on all targets.

Once you're done with debugging the templates, you can go back to the official templates with:

```dotnetcli
dotnet new install Uno.Templates
```
