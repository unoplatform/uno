# Pivot in Uno.UI

## Pivot UWP Default Style

Uno provides a full support of the Pivot UWP style.
The Pivot with the default style looks the same on all platforms, both statically and in motion.
If you need to have a custom design, you can just update the Pivot UWP default style for your needs.

## Pivot Native Default Style

With the 'NativeDefaultPivot' style on Android and iOS, however, the Pivot uses the native implementations of each platform.
Of course you can still bind to its properties in XAML as you normally would. 
This is another powerful option to have: for some apps it makes sense to look as 'native' as possible, for others it's desirable to have a rich, customised UI.
You may even want to mix and match different approaches for different screens in your app.

### Native Pivot Style for Android and iOS

If you want to use the native Pivot style for either Android or iOS, you'll need to specify the following `Pivot` style as well as a `NativePivotPresenter` style.

```xml
<!-- Default native Pivot styles (for both Android and iOS) -->
<xamarin:Style x:Key="NativeDefaultPivot"
                TargetType="Pivot">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Pivot">
                <NativePivotPresenter x:Name="NativePresenter" />
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</xamarin:Style>
```

#### NativePivotPresenter Android Style
Note that this one is written in C# rather than XAML because of the `SlidingTabLayout` object.

```csharp
var style = new Style(typeof(NativePivotPresenter))
{
    Setters = 
    {
        new Setter<NativePivotPresenter>("Template", pb => pb
            .Template = new ControlTemplate(() =>
                new Grid
                {
                    RowDefinitions = 
                    {
                        new RowDefinition(){ Height = GridLength.Auto},
                        new RowDefinition(){ Height = new GridLength(1, GridUnitType.Star)},
                    },

                    Children =
                    {
                        // Header
                        new Border
                        {
                            Child = new Uno.UI.Controls.SlidingTabLayout(ContextHelper.Current)
                            {
                                LayoutParameters = new Android.Views.ViewGroup.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.WrapContent),
                            },
                            BorderThickness = new Thickness(0,0,0,1),
                        }
                        .Apply(b => b.SetBinding("Background", new Binding { Path = "Background", RelativeSource = RelativeSource.TemplatedParent }))
                        .Apply(b => b.SetBinding("BorderBrush", new Binding { Path = "BorderBrush", RelativeSource = RelativeSource.TemplatedParent })),

                        // Content
                        new ExtendedViewPager(ContextHelper.Current)
                        {
                            OffscreenPageLimit = 1,
                            PageMargin = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 4, ContextHelper.Current.Resources.DisplayMetrics),
                            SwipeEnabled = true,
                        }
                        .Apply(v => Grid.SetRow(v, 1))
                    }
            })
        )
    }
};

Style.RegisterDefaultStyleForType(typeof(NativePivotPresenter), style);
```

#### NativePivotPresenter iOS Style

```xml
<ios:Style TargetType="NativePivotPresenter">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Pivot">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="1*" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
                    <Grid x:Name="PART_Content"
                            Grid.Row="0" />
                    <UITabBar Grid.Row="1" />
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</ios:Style>
```