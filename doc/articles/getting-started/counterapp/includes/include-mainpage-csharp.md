The layout for the `MainPage` is defined in the **MainPage.cs** file. This file contains the C# Markup that defines the layout of the application.

```csharp
namespace Uno;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(
                new StackPanel()
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        new TextBlock()
                            .Text("Hello Uno Platform!")
                    )
            );
    }
}

```
