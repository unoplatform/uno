---
uid: Uno.Development.FluentAssets
---

# Uno Fluent UI assets

Starting from Uno.UI 4.7, Uno Platform provides a cross platform fluent symbols font, provided by the [Uno.Fonts.Fluent](https://nuget.info/packages/Uno.Fonts.Fluent) NuGet package.

When included in the Uno Platform heads (excluding Windows and UWP heads), the symbols fonts will be used to display any control that makes use of it, such as the "burger" menu icon in a `NavigationView`.

## Usage

The symbol font is automatically used by built-in styles and templates. You can reference it in XAML using the `SymbolThemeFontFamily` resource. For example:

```xml
<FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" Glyph="&#xE117;"/>
```

## Upgrading to the Uno.Fonts.Fluent package from previous releases

In your Uno Platform projects:

- Add the [Uno.Fonts.Fluent](https://nuget.info/packages/Uno.Fonts.Fluent) package to all your app heads.
- You will also need to make small modifications to individual platforms.
- For all heads, in respective `.csproj` files remove all font files named `winjs-symbols.ttf` or `uno-fluentui-assets.ttf`.
- For iOS, Catalyst and macOS, the `info.plist` file should be updated for both platforms to remove the `UIAppFonts` block:

    ```xml
    <key>UIAppFonts</key>
    <array>
        <string>Fonts/uno-fluentui-assets.ttf</string>
    </array>
    ```

- For WebAssembly, remove the contents of `Font.css` related to `@font-face { font-family: "Symbols"; ...}`.

## Known issues

On iOS and macOS, the indeterminate state for a CheckBox is not the right color.

## Related Issues

- [3011](https://github.com/unoplatform/uno/issues/3011)
- [967](https://github.com/unoplatform/uno/issues/967)
