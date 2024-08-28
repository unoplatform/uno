---
uid: xref:Uno.Development.UnhandledExceptions
---

# Generic Unhandled Exceptions handler

Starting Uno Platform 5.4, we are generating a generic handler for `Application.UnhandledException`, in line with WinUI approach. When debugger is attached, unhandled framework exceptions will be logged in the debugger output by default and the debugger will automatically break. To avoid debugger breaking behavior, you can add the following into any `PropertyGroup` of your `.csproj`:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION</DefineConstants>
</PropertyGroup>
```
