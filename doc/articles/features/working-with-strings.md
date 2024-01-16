---
uid: Uno.Features.Localization
---

# String resources and localization

> [!TIP]
> Relevant tutorials on this topic:
>
> - [How to localize text resources](xref:Uno.Tutorials.Localization)
> - [How to change app language at runtime](xref:Uno.Tutorials.ChangeAppLanguage)

Localization is done through the `resw` files in the current project. Normally you would put these files in the `strings/[lang]` folder in the `[AppName]` project.  Resources are then referenced using [`x:Uid`](https://learn.microsoft.com/windows/uwp/xaml-platform/x-uid-directive).

See the UWP documentation on [localizing strings in your UI](https://learn.microsoft.com/windows/uwp/app-resources/localize-strings-ui-manifest).

Resources may be placed in the default scope files `Resources.resw`, or in a custom named file. Custom named file content
can be used with the `x:Uid="/myResources/MyResource"` format, see [how to factor strings into multiple resource files](https://learn.microsoft.com/windows/uwp/app-resources/localize-strings-ui-manifest#factoring-strings-into-multiple-resources-files).

Note that the default language can be defined using the `DefaultLanguage` msbuild property, using an IETF Language Tag (e.g. `en` or `fr-FR`).

By default, the app will use the operating system's language first, falling back to the `DefaultLanguage` second. If you would like to change localization at the app level, you can use the [`ApplicationLanguages.PrimaryLanguageOverride`](https://learn.microsoft.com/en-us/uwp/api/windows.globalization.applicationlanguages.primarylanguageoverride?view=winrt-22621) property:

```csharp
ApplicationLanguages.PrimaryLanguageOverride = "fr-CA"; // or, any IETF language tag
```

Note that changing the `PrimaryLanguageOverride` will not retroactively update any loaded pages. You will need to reload the page or navigate to a new one.
