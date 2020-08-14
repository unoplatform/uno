# Uno.UI and UWP API or Behavior differences

For legacy, platform support or performance reasons, Uno has some notable API differences.

## DependencyObject is an interface.
`DependencyObject` is an interface to allow for XAML controls to inherit directly from their native counterpart. The implementation of the methods is done through the `DependencyObjectGenerator` source generator, automatically.

This has some implications in generic constraints which require a class, but can be worked around using the `IS_UNO` define.

## ListView implementations

The implementations of the ListView for iOS and Android use the native controls for performance reasons, see the [ListViewBase implementation documentation](controls/ListViewBase.md#internal-implementation).

## Themes

`FrameworkElement.RequestedTheme` is supported in a very limited form. Setting `FrameworkElement.RequestedTheme` on *any* element will currently apply the nominated theme to the *entire* visual tree.

### Custom Themes

On Windows, there are some _themes_ that can target, but there is no way to trigger them. The most
known is the `HighContrast` theme.

You can do something similar - and even create totally custom themes - by using the following helper:

``` csharp
  // Set current theme to High contrast
  Uno.UI.RequestedCustomTheme = "HighContrast";
```

* Beware, all themes are **CASE SENSITIVE**.
* Themed dictionaries will fall back to `Application.Current.RequestedTheme` when they are not
  defining a resource for the custom theme.
* You can put any string and create totally custom themes, but they won't be supported by UWP.

Themes [are implemented](https://calculator.platform.uno?Theme=Pink) in the Uno port of the Windows 10 calculator. See [App.xaml.cs](https://github.com/unoplatform/calculator/blob/7772a593b541edd9809bc8946ee29d6a5b29e0ff/src/Calculator.Shared/App.xaml.cs#L79) and  [Styles.xaml](https://github.com/unoplatform/calculator/blob/7772a593b541edd9809bc8946ee29d6a5b29e0ff/src/Calculator.Shared/Styles.xaml).


