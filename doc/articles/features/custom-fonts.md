# Custom Fonts

## Custom Fonts on Android

Fonts must be placed in the `Assets` folder of the head project, matching the path of the fonts in Windows, and marked as `AndroidAsset`.
The format is the same as Windows, as follows:

```xml
<Setter Property="FontFamily" Value="/Assets/Fonts/Roboto-Regular.ttf#Roboto" />
```
   or
   
```xml
<Setter Property="FontFamily" Value="ms-appx:///Assets/Fonts/Roboto-Regular.ttf#Roboto" />
```

## Custom Fonts on iOS

Fonts must be placed in the `Resources/Fonts` folder of the head project, be marked as
`BundleResource` for the build type.

Each custom font **must** then be specified in the `info.plist` file as follows:

```xml
<key>UIAppFonts</key>
<array>
    <string>Fonts/yourfont01.ttf</string>
    <string>Fonts/yourfont02.ttf</string>
    <string>Fonts/yourfont03.ttf</string>
</array>
```

The format is the same as Windows, as follows:

```xml
<Setter Property="FontFamily" Value="/Assets/Fonts/yourfont01.ttf#Roboto" />
```
    or

```xml
<Setter Property="FontFamily" Value="ms-appx:///Assets/Fonts/yourfont01.ttf#Roboto" />
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
        Value="ms-appx:///Assets/Fonts/yourfont01.ttf#Roboto" />
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
        Value="ms-appx:///Assets/Fonts/yourfont01.ttf" />
```

Will match:

```css
@font-face {
  font-family: "yourfont01";
  ...
}
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
