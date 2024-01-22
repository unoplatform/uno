---
uid: Uno.SilverlightMigration.ConsideringNavigation
---

# Considering navigation

Silverlight 3 was the first version that included a navigation framework. Given the fact that Silverlight was predominately deployed via web browsers, it is unsurprising that the implementation mirrored the browser style of navigating to pages via URIs, passing query parameters and maintaining a navigation history for back/forward traversal.

Fortunately, UWP utilizes a similar conceptual approach, although the implementation differs - UWP navigates to a specific **Type** (e.g. `typeof(MainPage)`) rather than a **URI**. As Silverlight utilizes URI mapping and fragment navigation, this does result in a fundamental change to how navigation is defined in UWP, and the concept of defining routes via the **UriMapper** is not supported.

## The Frame control

Here is an example of how navigation is configured in the Silverlight **TimeEntryRIA** application using the **Frame** control:

```xml
<navigation:Frame x:Name="ContentFrame" Style="{StaticResource ContentFrameStyle}"
                    Source="/Home"
                    Navigated="ContentFrame_Navigated"
                    NavigationFailed="ContentFrame_NavigationFailed"
                    Navigating="ContentFrame_Navigating">
    <navigation:Frame.UriMapper>
        <uriMapper:UriMapper>
            <uriMapper:UriMapping Uri="" MappedUri="/Views/Home.xaml"/>
            <uriMapper:UriMapping Uri="/AddTimeEntry/{date}" MappedUri="/Views/AddTimeEntryPage.xaml?date={date}"/>
            <uriMapper:UriMapping Uri="/{pageName}" MappedUri="/Views/{pageName}.xaml"/>
        </uriMapper:UriMapper>
    </navigation:Frame.UriMapper>
</navigation:Frame>
```

The **Frame** control acts as a container for pages; it validates the state and maintains the navigation history and supports a number of events related to navigating. These events can be leveraged to, for example, verify the user signed in and has the correct role to view a page or handle a failed navigation, etc.

Within the **Frame** element, a **UriMapper** is defined. Similar to web applications with custom routes, the **UriMapper** converts a uniform resource identifier (URI) into a new URI based on the rules of a matching object specified in a collection of mapping objects. In this case, there is a default mapping for empty URIs that maps to the `/Views/Home.xaml` view, a specific mapping for a view, and then a general catch all. You can see how the use of tokens can be used to match pages or pass parameters, such as `{date}` can map a shorthand `/AddTimeEntry/1%2F18%2F2021` (URL encoded `/AddTimeEntry/1/18/2021`) to the explicit `/Views/AddTimeEntryPage.xaml?date=1%2F18%2F2021`

An example of navigating to **AddTimeEntry** in Silverlight Code would look like:

```csharp
var encodeDate =  Uri.EscapeDataString(_lastSelectedDate.Value.ToShortDateString());
this.NavigationService.Navigate(new Uri("/AddTimeEntry/" + encodeDate, UriKind.Relative));
```

UWP also uses a **Frame** control in a similar fashion, and is typically wrapped in another control, such as the **NavigationView**. Performing a similar navigation to the above in UWP would look like this:

```csharp
this.Navigate(typeof(AddTimeEntryPage), _lastSelectedDate.Value.ToShortDateString());
```

In UWP, there are 3 overloads of the **Navigate** method:

* **Navigate(Type)** - navigate to the specified Type
* **Navigate(Type, Object)** - navigate to the specified Type, passing a parameter object
* **Navigate(Type, Object, NavigationTransitionInfo)** - navigate to the specified Type, passing a parameter object and specifying an animated transition to use (if supported).

> [!IMPORTANT]
> Although the parameter is of type **Object**, it must have a basic type (string, char, numeric, or GUID) to support parameter serialization. More complex content can be passed if it serialized to/from JSON strings first.
>
> [!TIP]
> Full details for the **Navigate** method and overloads can be viewed here - [https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.frame.navigate](https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.frame.navigate)

## The navigation links

In Silverlight, there is no "out-of-the-box" control that renders a list of navigation links - it is instead common to use a horizontal **StackPanel** to layout some customized **HyperLink** controls:

```xml
<StackPanel x:Name="LinksStackPanel" Style="{StaticResource LinksStackPanelStyle}">

    <HyperlinkButton x:Name="Link1" Style="{StaticResource LinkStyle}"
                    NavigateUri="/Home" TargetName="ContentFrame" Content="{Binding Path=ApplicationStrings.HomePageTitle, Source={StaticResource ResourceWrapper}}"/>
    <Rectangle Style="{StaticResource DividerStyle}"/>
    <HyperlinkButton x:Name="Link2" Style="{StaticResource LinkStyle}"
                    NavigateUri="/TimeEntryPage" TargetName="ContentFrame" Content="{Binding Path=ApplicationStrings.TimeEntryNavTitle, Source={StaticResource ResourceWrapper}}"/>
    <Rectangle  Style="{StaticResource DividerStyle}"/>
    <HyperlinkButton x:Name="Link3" Style="{StaticResource LinkStyle}"
                    NavigateUri="/ReportsPage" TargetName="ContentFrame" Content="{Binding Path=ApplicationStrings.ReportsPageTitle, Source={StaticResource ResourceWrapper}}"/>
    <Rectangle  Style="{StaticResource DividerStyle}"/>
    <HyperlinkButton x:Name="Link4" Style="{StaticResource LinkStyle}"
                    NavigateUri="/AdminPage" TargetName="ContentFrame" Content="{Binding Path=ApplicationStrings.AdminPageTitle, Source={StaticResource ResourceWrapper}}"/>
    <Rectangle  Style="{StaticResource DividerStyle}"/>
    <HyperlinkButton x:Name="Link5" Style="{StaticResource LinkStyle}"
                    NavigateUri="/About" TargetName="ContentFrame" Content="{Binding Path=ApplicationStrings.AboutPageTitle, Source={StaticResource ResourceWrapper}}"/>
</StackPanel>
```

> [!NOTE]
> In the **HyperLink** button code above, the **Content** properties are bound to a string resource that can provide support for different languages. UWP also has a similar capability - you can learn more here [https://learn.microsoft.com/windows/uwp/app-resources/localize-strings-ui-manifest](https://learn.microsoft.com/windows/uwp/app-resources/localize-strings-ui-manifest).

As mentioned above, in UWP, the **NavigationView** and **Frame** controls are often used together to achieve a similar effect.

> [!TIP]
> You aren't constrained to just using a **NavigationView** and adopting the same layout/approach as the original Silverlight app. Uwp and Uno supports other types of app layouts and navigation styles with supporting controls. You can review these guidelines here:
>
> * [Navigation design basics for Windows apps](https://learn.microsoft.com/windows/uwp/design/basics/navigation-basics)

## Creating the navigation structure in Uno

In the next series of tasks, the Uno solution will be updated to implement the basic navigational structure. As there is no direct mapping between the Silverlight navigation controls and UWP, this will be writing new code. Once these tasks are completed, the app will be able to navigate to and from the various top-level pages detailed earlier.

> [!NOTE]
> At this stage, there will be no security or data retrieval, just simple headers.

### Adding the top-level pages

1. Return to the Uno solution in **Visual Studio**.

1. Locate the **[MyApp]** project in the solution.

1. Add a folder to the **[MyApp]** project and name it **Views**.

1. Right-click the **Views** folder and select **Add > New Item...**

    The **Add New Item** dialog is displayed.

1. On the left side of the **Add New Item** dialog, drill down into **Visual C#** and select **Uno Platform** to filter the list of item templates.

1. Select **Page (Uno Platform UWP)** and enter the name **HomePage.xaml**, and click **Add**.

    The new page should open displaying the XAML designer and the XAML code in a split-view.

    > [!TIP]
    > If the XAML designer does not open, in the Visual Studio **Tools** menu, select **Options**. Within the **Options** dialog, find the **XAML Designer > General** pane and ensure that the XAML designer is enabled and the **Default document view** is set to **Split View**. If you update the settings, click **OK** and restart Visual Studio.

1. The XAML for the **HomePage** view will be similar to:

    ```xml
    <Page
        x:Class="TimeEntryUno.Views.HomePage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:TimeEntryUno.Views"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

        <Grid>

        </Grid>
    </Page>
    ```

1. To add a simple header, add the following XAML between the `<Grid></Grid>` elements:

    ```xml
    <TextBlock Text="Home" Style="{StaticResource HeaderTextBlockStyle}"/>
    ```

1. Repeat the previous steps 4-8, adding new pages as detailed below, adjusting the **Text** of the **TextBlock** control as appropriate.

    * TimeEntryPage
    * ReportsPage
    * AdminPage
    * AboutPage

Now that the top-level pages have been added, it is time to configure the app to use navigation on startup.

## Next unit: Review app startup

[![button](assets/NextButton.png)](03-review-app-startup.md)
