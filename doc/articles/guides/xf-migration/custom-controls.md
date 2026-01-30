---
uid: Uno.XamarinFormsMigration.CustomControls
---

# Migrating Custom Controls from Xamarin.Forms to Uno Platform

This guide explores how to migrate custom controls from Xamarin.Forms to Uno Platform. As you migrate your Xamarin.Forms applications, you'll need to bring forward your custom controls. This article demonstrates how to port a Xamarin.Forms custom control to Uno Platform with practical examples.

## Understanding Custom Controls

A custom control in Xamarin.Forms consists of two files:

1. **XAML file**: Defines the UI
2. **Code-behind file**: Contains boilerplate code and custom properties
   
The class for your control inherits from `ContentView` and is marked as `partial` because some functionality is added by compiler-generated code. The standard empty class contains a constructor with a call to `InitializeComponent`, which loads the associated XAML file and defines any named members (marked with `x:Name` attributes) so you can refer to them in your code.

## Example: CardView Control

We'll walk through migrating the [CardView demo](https://github.com/xamarin/xamarin-forms-samples/tree/main/UserInterface/ContentViewDemos) from the Xamarin.Forms samples. The CardView is a sample control based on Xamarin.Forms' `ContentView`, designed to display an image, header, and body text in a card format (often used in a list).

## Data-Bindable Properties

### Xamarin.Forms Approach

In Xamarin.Forms, bindable properties are created as static instances of `BindableProperty`:

```csharp
public static readonly BindableProperty CardTitleProperty = 
    BindableProperty.Create(
        nameof(CardTitle), 
        typeof(string), 
        typeof(CardView), 
        string.Empty);
```

With this defined, you can set these properties from XAML when you create instances of your custom control, including data-binding them to your view model.

### Uno Platform Approach

In Uno and WinUI, the equivalent class is `DependencyProperty`, which has a static `Register` method with a very similar signature. You pass the default value in the constructor for `PropertyMetadata`:

```csharp
public static readonly DependencyProperty CardTitleProperty = 
    DependencyProperty.Register(
        nameof(CardTitle), 
        typeof(string), 
        typeof(CardView), 
        new PropertyMetadata(string.Empty));
```

### Instance Properties

To easily use these properties from code, define instance properties on the class that wrap and strongly type these binding properties:

**Xamarin.Forms and Uno Platform (same code):**

```csharp
public string CardTitle
{
    get => (string)GetValue(CardView.CardTitleProperty);
    set => SetValue(CardView.CardTitleProperty, value);
}
```

The Uno/WinUI `DependencyObject` contains the same `GetValue`/`SetValue` methods, so this code doesn't need to change.

## XAML Definition

### Root Element

**Xamarin.Forms:**

- Root element: `ContentView`
- Default namespace: `xmlns="http://xamarin.com/schemas/2014/forms"`

**Uno Platform:**

- Root element: `ContentControl`
- Default namespace: `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"`

### Control Name Mappings

Several common components exist with different names between these two flavors of XAML:

| Xamarin.Forms | Uno Platform / WinUI |
|---------------|---------------------|
| `Label` | `TextBlock` |
| `BoxView` | `Rectangle` |
| `Frame` | `Border` |
| `Entry` | `TextBox` |
| `Switch` | `ToggleSwitch` |
| `StackLayout` | `StackPanel` |
| `ContentView` | `ContentControl` |

### Size Properties

**Xamarin.Forms:**

- `HeightRequest` and `WidthRequest` to set preferred size

**Uno Platform:**

- `Height` and `Width` properties
- These may change based on layout constraints

### Color Properties

**Xamarin.Forms:**

- Often uses `Color` properties for controls

**Uno Platform:**

- Uses `Brush` properties, which can be solid colors, gradients, and more
- You can pass a `Color` as a property, and it's converted to a `SolidColorBrush` under the hood
- Preset colors are defined on the `Colors` type

**Accent Color:**

- Xamarin.Forms: `Color.Accent`
- Uno Platform: `{ThemeResource SystemAccentColor}` resource

### Alignment Properties

**Xamarin.Forms:**

- `HorizontalOptions` and `VerticalOptions` use the same enumeration
- Values: `Start`, `End`, `Fill`, `Center`

**Uno Platform:**

- Separate horizontal and vertical enumerations
- Properties are called `Alignment` rather than `Options`

Example:

- `VerticalOptions="Center"` becomes `VerticalAlignment="Center"`
- `HorizontalOptions="Start"` becomes `HorizontalAlignment="Left"`

### Font Properties

**Xamarin.Forms:**

- `FontAttributes` for bold or italic styling

**Uno Platform:**

- `FontWeight` for setting bold (e.g., `FontWeight="Bold"`)
- `FontStyle` for setting italic (e.g., `FontStyle="Italic"`)

## Complete CardView Migration

### Xamarin.Forms XAML

```xml
<ContentView xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="CardViewDemo.Controls.CardView"
             BindingContext="{x:Reference this}">
    <Frame CornerRadius="5" 
           HasShadow="True"
           BorderColor="Gray"
           BackgroundColor="White">
        <StackLayout>
            <Label Text="{Binding CardTitle}"
                   FontAttributes="Bold"
                   FontSize="Large"
                   HorizontalOptions="Center"
                   VerticalOptions="Center" />
            <BoxView Color="Gray" 
                     HeightRequest="1"
                     HorizontalOptions="Fill" />
            <Label Text="{Binding CardDescription}"
                   VerticalOptions="FillAndExpand"
                   HorizontalOptions="Fill" />
        </StackLayout>
    </Frame>
</ContentView>
```

### Uno Platform XAML

```xml
<ContentControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                x:Class="CardViewDemo.Controls.CardView"
                DataContext="{Binding RelativeSource={RelativeSource Self}}">
    <Border CornerRadius="5"
            BorderBrush="Gray"
            BorderThickness="1"
            Background="White">
        <Border.Shadow>
            <ThemeShadow />
        </Border.Shadow>
        <StackPanel>
            <TextBlock Text="{Binding CardTitle}"
                       FontWeight="Bold"
                       FontSize="24"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
            <Rectangle Fill="Gray"
                       Height="1"
                       HorizontalAlignment="Stretch" />
            <TextBlock Text="{Binding CardDescription}"
                       VerticalAlignment="Stretch"
                       HorizontalAlignment="Stretch"
                       TextWrapping="Wrap" />
        </StackPanel>
    </Border>
</ContentControl>
```

### Key Changes

1. **Root element**: `ContentView` → `ContentControl`
2. **Namespaces**: Changed to WinUI namespaces
3. **Frame**: `Frame` → `Border`
4. **Shadow**: `HasShadow="True"` → `<Border.Shadow><ThemeShadow /></Border.Shadow>`
5. **Layout**: `StackLayout` → `StackPanel`
6. **Text**: `Label` → `TextBlock`
7. **Divider**: `BoxView` → `Rectangle`
8. **Font**: `FontAttributes="Bold"` → `FontWeight="Bold"`
9. **Alignment**: `HorizontalOptions` → `HorizontalAlignment`
10. **Binding context**: `BindingContext="{x:Reference this}"` → `DataContext="{Binding RelativeSource={RelativeSource Self}}"`

## Code-Behind Migration

### Xamarin.Forms Code-Behind

```csharp
using Xamarin.Forms;

namespace CardViewDemo.Controls
{
    public partial class CardView : ContentView
    {
        public static readonly BindableProperty CardTitleProperty =
            BindableProperty.Create(nameof(CardTitle), typeof(string), typeof(CardView), string.Empty);

        public static readonly BindableProperty CardDescriptionProperty =
            BindableProperty.Create(nameof(CardDescription), typeof(string), typeof(CardView), string.Empty);

        public string CardTitle
        {
            get => (string)GetValue(CardTitleProperty);
            set => SetValue(CardTitleProperty, value);
        }

        public string CardDescription
        {
            get => (string)GetValue(CardDescriptionProperty);
            set => SetValue(CardDescriptionProperty, value);
        }

        public CardView()
        {
            InitializeComponent();
        }
    }
}
```

### Uno Platform Code-Behind

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CardViewDemo.Controls
{
    public partial class CardView : ContentControl
    {
        public static readonly DependencyProperty CardTitleProperty =
            DependencyProperty.Register(
                nameof(CardTitle), 
                typeof(string), 
                typeof(CardView), 
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CardDescriptionProperty =
            DependencyProperty.Register(
                nameof(CardDescription), 
                typeof(string), 
                typeof(CardView), 
                new PropertyMetadata(string.Empty));

        public string CardTitle
        {
            get => (string)GetValue(CardTitleProperty);
            set => SetValue(CardTitleProperty, value);
        }

        public string CardDescription
        {
            get => (string)GetValue(CardDescriptionProperty);
            set => SetValue(CardDescriptionProperty, value);
        }

        public CardView()
        {
            InitializeComponent();
        }
    }
}
```

### Key Changes in Code

1. **Namespaces**: `Xamarin.Forms` → `Microsoft.UI.Xaml` and `Microsoft.UI.Xaml.Controls`
2. **Base class**: `ContentView` → `ContentControl`
3. **Property type**: `BindableProperty` → `DependencyProperty`
4. **Registration**: `Create()` → `Register()` with `PropertyMetadata`
5. **Instance properties**: No changes needed

## Applying Control Templates

In the CardView sample, one of the views shows how the appearance of the custom control can be overridden by applying a new control template. This is possible with any `ContentControl`-derived control.

### Xamarin.Forms Template

```xml
<controls:CardView.ControlTemplate>
    <ControlTemplate>
        <Frame BackgroundColor="DarkGray" CornerRadius="10">
            <StackLayout>
                <Label Text="{TemplateBinding CardTitle}"
                       TextColor="White"
                       FontSize="Large" />
                <Label Text="{TemplateBinding CardDescription}"
                       TextColor="White" />
            </StackLayout>
        </Frame>
    </ControlTemplate>
</controls:CardView.ControlTemplate>
```

### Uno Platform Template

```xml
<controls:CardView.Template>
    <ControlTemplate TargetType="controls:CardView">
        <Border Background="DarkGray" CornerRadius="10">
            <StackPanel>
                <TextBlock Text="{TemplateBinding CardTitle}"
                           Foreground="White"
                           FontSize="24" />
                <TextBlock Text="{TemplateBinding CardDescription}"
                           Foreground="White" />
            </StackPanel>
        </Border>
    </ControlTemplate>
</controls:CardView.Template>
```

### Key Differences in Templates

1. **Property name**: `ControlTemplate` → `Template`
2. **TargetType**: Must be specified in Uno Platform (enables IntelliSense)
3. **Control mappings**: Same as before (`Frame` → `Border`, etc.)

## Shadows

### Xamarin.Forms

```xml
<Frame HasShadow="True">
    <!-- Content -->
</Frame>
```

### Uno Platform

```xml
<Border>
    <Border.Shadow>
        <ThemeShadow />
    </Border.Shadow>
    <!-- Content -->
</Border>
```

In code-behind, you must also set a translation on the Border to raise it in the Z-axis:

```csharp
#if !HAS_UNO_WINUI
    CardBorder.Translation += new System.Numerics.Vector3(0, 0, 32);
#endif
```

> [!NOTE]
> The Z-translation for shadows is not currently supported in Uno on all platforms. The conditional compilation ensures the code only runs where supported.

## Platform-Specific Code

If you need platform-specific behavior, use conditional compilation:

```csharp
#if __ANDROID__
    // Android-specific code
#elif __IOS__
    // iOS-specific code
#elif __WASM__
    // WebAssembly-specific code
#elif WINDOWS
    // Windows-specific code
#endif
```

## Using the Custom Control

Once migrated, use your custom control the same way in XAML:

```xml
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:controls="using:CardViewDemo.Controls">
    
    <StackPanel Spacing="10" Padding="20">
        <controls:CardView CardTitle="First Card"
                          CardDescription="This is the first card"/>
        <controls:CardView CardTitle="Second Card"
                          CardDescription="This is the second card"/>
    </StackPanel>
</Page>
```

## Migration Checklist

When migrating custom controls from Xamarin.Forms to Uno Platform:

- [ ] Change root element from `ContentView` to `ContentControl`
- [ ] Update XML namespaces to WinUI
- [ ] Change `BindableProperty.Create()` to `DependencyProperty.Register()`
- [ ] Update control names (`Label` → `TextBlock`, `Frame` → `Border`, etc.)
- [ ] Change `HeightRequest`/`WidthRequest` to `Height`/`Width`
- [ ] Update `Color` properties to `Brush` properties (often automatic)
- [ ] Change `HorizontalOptions`/`VerticalOptions` to `HorizontalAlignment`/`VerticalAlignment`
- [ ] Update `FontAttributes` to `FontWeight` and `FontStyle`
- [ ] Change `HasShadow` to `<Border.Shadow><ThemeShadow /></Border.Shadow>`
- [ ] Update using statements in code-behind
- [ ] Test on all target platforms
- [ ] Add conditional compilation for platform-specific features

## Common Pitfalls

1. **Forgetting TargetType**: Control templates in Uno Platform must specify `TargetType`
2. **Shadow Z-translation**: Shadows require Z-translation that isn't supported everywhere
3. **Color vs. Brush**: While often automatic, be aware of the difference
4. **Alignment values**: `Start`/`End` in Xamarin.Forms → `Left`/`Right` in Uno Platform
5. **BindingContext vs. DataContext**: Remember to update binding context references

## Summary

Migrating custom controls from Xamarin.Forms to Uno Platform involves:

- Changing base class from `ContentView` to `ContentControl`
- Converting `BindableProperty` to `DependencyProperty`
- Updating XAML control names and property names
- Adjusting for WinUI API differences

The structure remains very similar, and much of the logic can be reused with minimal changes. The XAML flavor is similar enough that migration is straightforward once you understand the key differences.

## Next Steps

- Continue with [Migrating Custom-Drawn Controls](xref:Uno.XamarinFormsMigration.CustomDrawnControls)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)

## Sample Code

The complete sample project showing how to migrate a Xamarin.Forms custom control to Uno Platform is available in the [Uno.Samples repository](https://github.com/unoplatform/Uno.Samples/tree/master/UI/CardViewMigration).
