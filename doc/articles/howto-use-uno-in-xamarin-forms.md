---
uid: Uno.Development.UseUnoControlsInXamarinForms
---

# How to use Uno Platform Controls in Xamarin.Forms

All Uno Platform UI controls inherit directly from native views, which makes for an easy way for its controls to be integrated in Xamarin.iOS and Xamarin.Android projects.

Xamarin.Forms provides a way to use native-inheriting views to be added as part of its visual tree (both [in code-behind](https://learn.microsoft.com/xamarin/xamarin-forms/platform/native-views/code) and [in XAML](https://learn.microsoft.com/xamarin/xamarin-forms/platform/native-views/xaml)).

## Initialize

In code-behind, before adding Uno.UI controls to the visual tree or in your Xamarin.Forms App constructor you should initialize Uno UI.

Example for Android:

```csharp
public App()
{
#if __ANDROID__
    // set current Activity. You may use 'Plugin.CurrentActivity' NuGet package
    Uno.UI.ContextHelper.Current = Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity;
    // or if you use Xamarin.Essentials v1.4.0 and higher
    Uno.UI.ContextHelper.Current = Xamarin.Essentials.Platform.CurrentActivity;
#endif
    // create an instance of Application, otherwise Uno.UI controls won't work in Xamarin.Forms
    new Windows.UI.Xaml.Application();

    InitializeComponent();
    MainPage = new MainPage();
}

```

## Add Uno views in Xamarin.Forms XAML

Xamarin.Forms supports adding views directly in XAML documents, using platform specific namespaces:

```xml
<ContentPage xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:UnoFormsApp"
             xmlns:ios="clr-namespace:Windows.UI.Xaml.Controls;assembly=Uno.UI;targetPlatform=iOS"
             xmlns:android="clr-namespace:Windows.UI.Xaml.Controls;assembly=Uno.UI;targetPlatform=Android"
             x:Class="UnoFormsApp.MainPage">

    <StackLayout Margin="50">
        <!-- Place new controls here -->
        <Label Text="Xamarin.Forms Label"  />
        <ios:TextBlock Text="iOS Uno Platform TextBlock" />
        <android:TextBlock Text="Android Uno Platform TextBlock" />
    </StackLayout>

</ContentPage>
```

## Add Uno Views from C# code

From the code-behind, it's possible to use a ContentView control to host the Uno Platform controls:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:UnoFormsApp"
             x:Class="UnoFormsApp.MainPage">
    <Grid Margin="50">
        <Label Text="Xamarin.Forms Label"  />
        <ContentView x:Name="myContent"/>
    </Grid>
</ContentPage>
```

and the corresponding C# code:

```csharp
using Windows.UI.Xaml.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace UnoFormsApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            myContent.Content = new ContentControl {
                Content = new Border {
                    BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gray),
                    BorderThickness = new Windows.UI.Xaml.Thickness(1),
                    Child = new Pivot {
                        Items=
                        {
                            new PivotItem
                            {
                                Header = "UWP Item 1",
                                Content = "Content 1"
                            },
                            new PivotItem
                            {
                                Header = "UWP Item 2",
                                Content = "Content 2"
                            }
                        }
                    }
                }
            }.ToView();
        }
    }
}
```
