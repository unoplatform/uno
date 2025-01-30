---
uid: Uno.Development.UnhandledExceptions
---

# Generic Unhandled Exceptions handler

Starting Uno Platform 5.4, we are generating a generic handler for `Application.UnhandledException`, in line with WinUI approach. When debugger is attached, unhandled framework exceptions will be logged in the debugger output by default. If you also want the debugger to break on unhandled exceptions, set the `BreakOnUnhandledExceptions` property to `true` in the `.csproj`:

```xml
<PropertyGroup>
  <BreakOnUnhandledExceptions>true</BreakOnUnhandledExceptions>
</PropertyGroup>
```

If you want to disable the generated `Application.UnhandledException` handler altogether, define the `DISABLE_GENERATED_UNHANDLED_EXCEPTION_HANDLER` constant in `.csproj` instead:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DISABLE_GENERATED_UNHANDLED_EXCEPTION_HANDLER</DefineConstants>
</PropertyGroup>
```
