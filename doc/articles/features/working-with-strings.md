# String resources and localization

Localization is done through the `resw` files in the current project. Normally you would put these files in the `strings/[lang]` folder in the `[AppName].Shared` project.  Resources are then referenced using [`x:Uid`](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-uid-directive).

See the UWP documentation on [localizing strings in your UI](https://docs.microsoft.com/en-us/windows/uwp/app-resources/localize-strings-ui-manifest).

Resources may be placed in the default scope files `Resources.resw`, or in a custom named file. Custom named file content
can be used with the `x:Uid="/myResources/MyResource"` format, see [how to factor strings into multiple resource files](https://docs.microsoft.com/en-us/windows/uwp/app-resources/localize-strings-ui-manifest#factoring-strings-into-multiple-resources-files).

Note that the default language can be defined using the `DefaultLanguage` msbuild property, using an IETF Language Tag (e.g. `en` or `fr-FR`).