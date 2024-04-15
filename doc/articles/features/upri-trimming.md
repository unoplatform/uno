---
uid: Uno.Features.StringResourceTrimming
---

# String Resource Trimming

String resource trimming is an optional feature used to reduce the final payload size of an Uno Platform application by removing unused localization languages throughout the app.

This feature is currently available for WebAssembly projects.

## Using String Resource Trimming

To enable resources trimming, you need to declare which culture(s) your application uses. For example, in your csproj add:

```xml
<ItemGroup>
    <UnoSupportedLanguage Include="en-US" />
    <UnoSupportedLanguage Include="fr-CA" />
</ItemGroup>
```

You do not need to include parent cultures (`en` and `fr` in the previous example), they are automatically kept because they may be used as a fallback.

Make sure to update [Uno.Wasm.Bootstrap](https://www.nuget.org/packages/Uno.Wasm.Bootstrap) to the latest 7.0.x or 8.0.x version.
