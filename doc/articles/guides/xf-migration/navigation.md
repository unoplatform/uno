---
uid: Uno.XamarinFormsMigration.Navigation
---

# Migrating Navigation from Xamarin.Forms to Uno Platform

This guide explains how to migrate navigation patterns from Xamarin.Forms to Uno Platform. While the underlying concepts are similar, the APIs and approaches differ between the two frameworks.

## Navigation Fundamentals

### Xamarin.Forms Navigation

Xamarin.Forms uses a stack-based navigation model with the `INavigation` interface:
- Navigation is accessed via `Navigation` property on pages
- `NavigationPage` provides the navigation chrome (back button, title bar)
- Async/await pattern for all navigation operations

### Uno Platform Navigation

Uno Platform uses the WinUI `Frame` control for navigation:

- `Frame` maintains the navigation stack
- Navigation is synchronous (no await needed)
- Pages are navigated to by `Type` rather than instance

## Basic Navigation Patterns

### Navigate Forward

**Xamarin.Forms:**

```csharp
await Navigation.PushAsync(new DetailPage());
```

**Uno Platform:**

```csharp
Frame.Navigate(typeof(DetailPage));
```

### Navigate Back

**Xamarin.Forms:**

```csharp
await Navigation.PopAsync();
```

**Uno Platform:**

```csharp
if (Frame.CanGoBack)
{
    Frame.GoBack();
}
```

### Navigate to Root

**Xamarin.Forms:**

```csharp
await Navigation.PopToRootAsync();
```

**Uno Platform:**

```csharp
while (Frame.CanGoBack)
{
    Frame.GoBack();
}
// Or clear and navigate to root page
Frame.BackStack.Clear();
Frame.Navigate(typeof(MainPage));
```

## Passing Parameters

### Simple Parameters

**Xamarin.Forms:**

```csharp
var detailPage = new DetailPage(itemId);
await Navigation.PushAsync(detailPage);
```

**Uno Platform:**

```csharp
Frame.Navigate(typeof(DetailPage), itemId);
```

To receive the parameter in the target page:

```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    
    if (e.Parameter is int itemId)
    {
        // Use the parameter
        LoadItem(itemId);
    }
}
```

### Complex Parameters

**Xamarin.Forms:**

```csharp
var parameters = new NavigationParameters
{
    ItemId = 123,
    Mode = EditMode.Edit
};
await Navigation.PushAsync(new DetailPage(parameters));
```

**Uno Platform:**

```csharp
var parameters = new NavigationParameters
{
    ItemId = 123,
    Mode = EditMode.Edit
};
Frame.Navigate(typeof(DetailPage), parameters);
```

## Navigation Without Animation

**Xamarin.Forms:**

```csharp
await Navigation.PushAsync(new DetailPage(), animated: false);
await Navigation.PopAsync(animated: false);
```

**Uno Platform:**

```csharp
Frame.Navigate(typeof(DetailPage), null, 
    new SuppressNavigationTransitionInfo());

Frame.GoBack(new SuppressNavigationTransitionInfo());
```

## Managing the Back Stack

### Remove a Page from Stack

**Xamarin.Forms:**

```csharp
Navigation.RemovePage(pageToRemove);
```

**Uno Platform:**

```csharp
// Remove specific page by index
Frame.BackStack.RemoveAt(index);

// Remove all entries
Frame.BackStack.Clear();

// Remove last entry
if (Frame.BackStack.Count > 0)
{
    Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
}
```

### Insert Page into Stack

**Xamarin.Forms:**

```csharp
Navigation.InsertPageBefore(newPage, existingPage);
```

**Uno Platform:**

```csharp
// Insert a page entry into the back stack
var entry = new PageStackEntry(typeof(NewPage), parameter, null);
Frame.BackStack.Insert(index, entry);
```

## Modal Navigation

### Present Modal

**Xamarin.Forms:**

```csharp
await Navigation.PushModalAsync(new LoginPage());
```

**Uno Platform:**

```csharp
// Option 1: Use ContentDialog for modal dialogs
var dialog = new LoginDialog();
await dialog.ShowAsync();

// Option 2: Navigate in a child Frame
var modalFrame = new Frame();
modalFrame.Navigate(typeof(LoginPage));
// Add modalFrame to visual tree in an overlay
```

### Dismiss Modal

**Xamarin.Forms:**

```csharp
await Navigation.PopModalAsync();
```

**Uno Platform:**

```csharp
// For ContentDialog
dialog.Hide();

// For child Frame navigation
// Remove the frame from the visual tree
```

## Shell Navigation

Xamarin.Forms Shell provides URI-based navigation. For Uno Platform, you have several options:

### Option 1: Uno.Extensions.Navigation

The recommended approach is to use `Uno.Extensions.Navigation` which provides similar URI-based routing:

```csharp
// Register routes
services.AddSingleton<INavigator, Navigator>();

// Navigate by route
await navigator.NavigateRouteAsync(this, "app/details/123");
```

### Option 2: Frame Navigation

For simpler apps, use Frame directly with a navigation service:

```csharp
public class NavigationService : INavigationService
{
    private Frame _frame;
    
    public void Navigate(Type pageType, object parameter = null)
    {
        _frame.Navigate(pageType, parameter);
    }
    
    public void GoBack()
    {
        if (_frame.CanGoBack)
            _frame.GoBack();
    }
}
```

## TabbedPage Migration

**Xamarin.Forms:**

```xml
<TabbedPage xmlns="http://xamarin.com/schemas/2014/forms">
    <ContentPage Title="Tab 1" />
    <ContentPage Title="Tab 2" />
    <ContentPage Title="Tab 3" />
</TabbedPage>
```

**Uno Platform - Using NavigationView:**

```xml
<NavigationView PaneDisplayMode="Top">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Tab 1" Tag="Tab1Page"/>
        <NavigationViewItem Content="Tab 2" Tag="Tab2Page"/>
        <NavigationViewItem Content="Tab 3" Tag="Tab3Page"/>
    </NavigationView.MenuItems>
    <Frame x:Name="ContentFrame"/>
</NavigationView>
```

**Uno Platform - Using TabView (simpler):**

```xml
<TabView>
    <TabViewItem Header="Tab 1">
        <local:Tab1Content />
    </TabViewItem>
    <TabViewItem Header="Tab 2">
        <local:Tab2Content />
    </TabViewItem>
    <TabViewItem Header="Tab 3">
        <local:Tab3Content />
    </TabViewItem>
</TabView>
```

## MasterDetailPage / FlyoutPage Migration

**Xamarin.Forms:**

```xml
<FlyoutPage>
    <FlyoutPage.Flyout>
        <ContentPage Title="Menu">
            <!-- Menu content -->
        </ContentPage>
    </FlyoutPage.Flyout>
    <FlyoutPage.Detail>
        <NavigationPage>
            <x:Arguments>
                <local:MainPage />
            </x:Arguments>
        </NavigationPage>
    </FlyoutPage.Detail>
</FlyoutPage>
```

**Uno Platform - Using NavigationView:**

```xml
<NavigationView x:Name="NavView" 
                PaneDisplayMode="Left"
                IsBackButtonVisible="Collapsed">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Icon="Home" Tag="HomePage"/>
        <NavigationViewItem Content="Settings" Icon="Setting" Tag="SettingsPage"/>
    </NavigationView.MenuItems>
    
    <Frame x:Name="ContentFrame"/>
</NavigationView>
```

With code-behind for navigation:

```csharp
private void NavView_ItemInvoked(NavigationView sender, 
    NavigationViewItemInvokedEventArgs args)
{
    if (args.InvokedItemContainer?.Tag is string tag)
    {
        Type pageType = tag switch
        {
            "HomePage" => typeof(HomePage),
            "SettingsPage" => typeof(SettingsPage),
            _ => null
        };
        
        if (pageType != null)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
```

## Navigation Events

### Xamarin.Forms

```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    // Page is about to appear
}

protected override void OnDisappearing()
{
    base.OnDisappearing();
    // Page is about to disappear
}
```

### Uno Platform

```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    // Page has been navigated to
    // e.Parameter contains the navigation parameter
    // e.NavigationMode indicates forward, back, or refresh
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    base.OnNavigatedFrom(e);
    // Page is being navigated away from
}

protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
{
    base.OnNavigatingFrom(e);
    // Navigation is about to occur - can be cancelled
    if (HasUnsavedChanges)
    {
        e.Cancel = true;
        ShowSaveDialog();
    }
}
```

## Navigation with MVVM

### Using Commands for Navigation

**View Model:**

```csharp
public class MainViewModel
{
    private readonly INavigationService _navigationService;
    
    public ICommand NavigateToDetailsCommand { get; }
    
    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        NavigateToDetailsCommand = new RelayCommand<int>(NavigateToDetails);
    }
    
    private void NavigateToDetails(int itemId)
    {
        _navigationService.Navigate(typeof(DetailPage), itemId);
    }
}
```

### Using Uno.Extensions.Navigation with MVUX

```csharp
public partial record MainModel(INavigator Navigator)
{
    public async ValueTask GoToDetails(int itemId)
    {
        await Navigator.NavigateViewModelAsync<DetailViewModel>(
            this, 
            data: new DetailData(itemId));
    }
}
```

## Deep Linking

### Xamarin.Forms Shell

```csharp
Routing.RegisterRoute("details", typeof(DetailPage));
await Shell.Current.GoToAsync("details?id=123");
```

### Uno Platform with Uno.Extensions

```csharp
// Configure routes in App.cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    RegisterRoutes();
}

private void RegisterRoutes()
{
    RouteData.Register("details", typeof(DetailPage));
}

// Navigate using URI
await navigator.NavigateRouteAsync(this, "details?id=123");
```

## Navigation Transition Animations

### Custom Transitions

**Uno Platform:**

```csharp
// Slide in from right
Frame.Navigate(typeof(DetailPage), null, 
    new SlideNavigationTransitionInfo
    {
        Effect = SlideNavigationTransitionEffect.FromRight
    });

// Drill in
Frame.Navigate(typeof(DetailPage), null, 
    new DrillInNavigationTransitionInfo());

// Common continue
Frame.Navigate(typeof(DetailPage), null, 
    new CommonNavigationTransitionInfo());
```

## Migration Checklist

When migrating navigation:

- [ ] Replace `Navigation.PushAsync()` with `Frame.Navigate()`
- [ ] Replace `Navigation.PopAsync()` with `Frame.GoBack()`
- [ ] Update parameter passing to use `NavigationEventArgs.Parameter`
- [ ] Implement `OnNavigatedTo` instead of `OnAppearing`
- [ ] Replace modal navigation with `ContentDialog` or overlay patterns
- [ ] Migrate Shell routes to Uno.Extensions.Navigation or custom routing
- [ ] Replace `TabbedPage` with `TabView` or `NavigationView`
- [ ] Replace `FlyoutPage` with `NavigationView`
- [ ] Update navigation events and lifecycle methods
- [ ] Test back button behavior on all platforms

## Common Patterns

### Confirm Before Navigation

**Xamarin.Forms:**

```csharp
protected override bool OnBackButtonPressed()
{
    if (HasUnsavedChanges)
    {
        ShowSaveDialog();
        return true; // Prevent back navigation
    }
    return base.OnBackButtonPressed();
}
```

**Uno Platform:**

```csharp
protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
{
    base.OnNavigatingFrom(e);
    
    if (HasUnsavedChanges)
    {
        e.Cancel = true;
        ShowSaveDialog();
    }
}
```

### Pass Data Back on Navigation

**Xamarin.Forms:**

```csharp
// In detail page
MessagingCenter.Send(this, "ItemUpdated", updatedItem);
await Navigation.PopAsync();

// In source page
MessagingCenter.Subscribe<DetailPage, Item>(this, "ItemUpdated", 
    (sender, item) => RefreshItem(item));
```

**Uno Platform:**

```csharp
// Option 1: Use navigation parameter on back navigation
// Store data in a service or shared state

// Option 2: Use event aggregator or messenger
WeakReferenceMessenger.Default.Send(new ItemUpdatedMessage(updatedItem));
Frame.GoBack();

// In source page
WeakReferenceMessenger.Default.Register<ItemUpdatedMessage>(this,
    (r, m) => RefreshItem(m.Item));
```

## Summary

Key differences when migrating navigation:

- **Synchronous vs Async**: Frame navigation is synchronous, no `await` needed
- **Type vs Instance**: Navigate to page types, not instances
- **Parameter handling**: Parameters passed separately, received in `OnNavigatedTo`
- **Back stack**: Managed via `Frame.BackStack` collection
- **Modal patterns**: Use `ContentDialog` or overlay patterns instead of modal pages
- **Shell navigation**: Use Uno.Extensions.Navigation for routing-based navigation

## Next Steps

- [Migrating Control Mappings](xref:Uno.XamarinFormsMigration.ControlMappings)
- [Migrating Data Binding](xref:Uno.XamarinFormsMigration.DataBinding)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)

## See Also

- [Uno.Extensions.Navigation Documentation](https://aka.platform.uno/uno-extensions-navigation)
- [Frame Class (WinUI)](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.frame)
- [NavigationView Control](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.navigationview)
