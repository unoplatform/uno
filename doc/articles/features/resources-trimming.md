---
uid: Uno.Features.ResourcesTrimming
---

# XAML Resource Trimming

XAML Resource and Binding trimming is an optional feature used to reduce the size of the final payload of an Uno Platform application.

The trimming phase happens after the compilation phase and tries to determine which UI controls are not used explicitly, and removes the associated XAML styles. The XAML styles are found through the value specified in the `TargetType` attribute.

As of Uno Platform 6.0, XAML Resources Trimming is available for apps targeting WebAssembly, iOS, and Desktop.

## Using XAML Resources trimming for applications

In order for an application to enable resources trimming, the following needs to be added to all projects of your solution that reference the Uno.WinUI (or Uno.UI) package, as well as the WebAssembly head project:

```xml
<PropertyGroup>
    <UnoXamlResourcesTrimming>true</UnoXamlResourcesTrimming>
</PropertyGroup>
```

Make sure to update your dependencies:

- If you're using the .NET SDK 8.0.200 or later, you'll need to use the [Uno.Wasm.Bootstrap](https://www.nuget.org/packages/Uno.Wasm.Bootstrap) package 8.0.9 or later.
- With .NET SDK 8.0.10x or earlier, you will also need to add the following package to your `.Wasm.csproj`:

    ```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.NET.ILLink.Tasks" Version="8.0.0" />
    </ItemGroup>
    ```

## Enabling XAML Resources trimming for libraries and NuGet Packages

For libraries to be eligible for resources trimming, the `UnoXamlResourcesTrimming` tag must also be added.

## Troubleshooting

### Aggressive trimming

The XAML trimming phase may remove controls for which the use cannot be detected statically.

For instance, if your application relies on the `XamlReader` class, trimmed controls will not be available and will fail to load.

If XAML trimming is still needed, the [IL Linker configuration](xref:uno.articles.features.illinker) can be adjusted to keep controls individually or by namespace.

### Size is not reduced even if enabled

The IL Linker tool is used to implement this feature, and can be [controlled with its configuration file](xref:uno.articles.features.illinker).

For instance, if the linker configuration file contains `<assembly fullname="uno.ui" />`, none of the UI Controls will be excluded, and the final app size will remain close as without trimming.

## Size reduction statistics

As of Uno Platform 6.0, for a `dotnet new unoapp` created app:

|                      | without XAML Trimming | with XAML Trimming |
| -------------------- | --------------------- | ------------------ |
| Total IL Payload     |               12.9 MB |            9.12 MB |
| dotnet.wasm          |                 53 MB |            28.9 MB |
| iOS IPA              |                 93 MB |               73MB |
| Win32 Desktop        |                200 MB |              52 MB |
| macOS Desktop        |                200 MB |              58 MB |
