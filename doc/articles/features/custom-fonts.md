# Custom Fonts

The `FontFamily` property allows you to customize the font used in your application's UI. Please note that in the following examples, `yourfont.ttf` is a placeholder for the font file name, and `Your Font Name` is a placeholder for its actual name. Use a font management app to make figuring out the correct format easier. The free application, [Character Map](https://www.microsoft.com/store/productId/9WZDNCRDXF41), can be used to extract the full string for your selected font:

![Character Map UWP providing font information](../Assets/features/customfonts/charactermapuwp.png)

Following are specific guides on how custom font files should be provided for each target platform.

## Custom Fonts on Android

Fonts must be placed in the `Assets` folder of the head project, matching the path of the fonts in Windows, and marked as `AndroidAsset`.
The format is the same as Windows:

```xml
<Setter Property="FontFamily" Value="/Assets/Fonts/yourfont.ttf#Your Font Name" />
```
   or

```xml
<Setter Property="FontFamily" Value="ms-appx:///Assets/Fonts/yourfont.ttf#Your Font Name" />
```

## Custom Fonts on iOS

Fonts must be placed in the `Resources/Fonts` folder of the head project, be marked as
`BundleResource` for the build type.

Each custom font **must** then be specified in the `info.plist` file as follows:

```xml
<key>UIAppFonts</key>
<array>
    <string>Fonts/yourfont.ttf</string>
    <string>Fonts/yourfont02.ttf</string>
    <string>Fonts/yourfont03.ttf</string>
</array>
```

The format is the same as Windows, as follows:

```xml
<Setter Property="FontFamily" Value="/Assets/Fonts/yourfont.ttf#Your Font Name" />
```
    or

```xml
<Setter Property="FontFamily" Value="ms-appx:///Assets/Fonts/yourfont.ttf#Your Font Name" />
```

## Custom fonts on WebAssembly

Adding a custom font is done through the use of WebFonts, using a data-URI:

```css
@font-face {
  font-family: "Symbols";
  /* winjs-symbols.woff2: https://github.com/Microsoft/fonts/tree/master/Symbols */
  src: url(data:application/x-font-woff;charset=utf-8;base64,d09GMgABAAA...) format('woff');
}
```

This type of declaration is required to avoid measuring errors if the font requested by a `TextBlock` or a `FontIcon` needs to be downloaded first. Specifying it using a data-URI ensures the font is readily available.

The font names are referenced based on the `#` name, so:

```xml
<Setter Property="FontFamily"
        Value="ms-appx:///Assets/Fonts/yourfont.ttf#Your Font Name" />
```

Will match the following `@font-face`:

```css
@font-face {
  font-family: "Roboto";
  ...
}
```

In case your `FontFamily` value does not contain `#`, Uno falls back to the font file name. Hence for:

```xml
<Setter Property="FontFamily"
        Value="ms-appx:///Assets/Fonts/yourfont.ttf" />
```

Will match:

```css
@font-face {
  font-family: "yourfont";
  ...
}
```
## Custom Fonts on macOS

Fonts must be placed in the `Resources/Fonts` folder of the head project, be marked as
`BundleResource` for the build type.

The fonts location path   **must** then be specified in the `info.plist` file as follows:

```xml
<key>ATSApplicationFontsPath</key>
<string>Fonts</string>
```

> [!IMPORTANT]
> Please note that unlike iOS, for macOS only the path is specified. There is no need to list each font independently.

The format is the same as Windows, as follows:

```xml
<Setter Property="FontFamily" Value="/Assets/Fonts/yourfont.ttf#Your Font Name" />
```

    or

```xml
<Setter Property="FontFamily" Value="ms-appx:///Assets/Fonts/yourfont.ttf#Your Font Name" />
```

## Custom Fonts Notes

Please note that some custom fonts need the FontFamily and FontWeight properties to be set at the same time in order to work properly on TextBlocks, Runs and for styles Setters.
If that's your case, here are some examples of code:

```xml
<FontFamily x:Key="FontFamilyLight">ms-appx:///Assets/Fonts/PierSans-Light.otf#Pier Sans Light</FontFamily>
<FontFamily x:Key="FontFamilyBold">ms-appx:///Assets/Fonts/PierSans-Bold.otf#Pier Sans Bold</FontFamily>

<Style x:Key="LightTextBlockStyle"
	   TargetType="TextBlock">
	<Setter Property="FontFamily"
			Value="{StaticResource FontFamilyLight}" />
	<Setter Property="FontWeight"
			Value="Light" />
	<Setter Property="FontSize"
			Value="16" />
</Style>

<Style x:Key="BoldTextBlockStyle"
	   TargetType="TextBlock">
	<Setter Property="FontFamily"
			Value="{StaticResource FontFamilyBold}" />
	<Setter Property="FontWeight"
			Value="Bold" />
	<Setter Property="FontSize"
			Value="24" />
</Style>

<TextBlock Text="TextBlock with Light FontFamily and FontWeight."
		   FontFamily="{StaticResource FontFamilyLight}"
		   FontWeight="Light" />

<TextBlock Style="{StaticResource BoldTextBlockStyle}">
	<Run Text="TextBlock with Runs" />
	<Run Text="and  Light FontFamily and FontWeight for the second Run."
		 FontWeight="Light"
		 FontFamily="{StaticResource FontFamilyLight}" />
</TextBlock>
```
