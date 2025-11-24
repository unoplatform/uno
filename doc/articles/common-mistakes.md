---
uid: Uno.CommonMistakes
---

# Common Mistakes and How to Avoid Them

This guide highlights frequent pitfalls when developing with Uno Platform and provides practical solutions to help you avoid them.

## Project Setup Mistakes

### ❌ Using the Wrong .NET Version

**Problem**: Starting a project with an older .NET version or not understanding support timelines.

**Why it happens**: Not aware of .NET LTS vs STS support or using default templates without checking versions.

**Solution**:
- **Use .NET 10.0 (LTS)** for new projects requiring long-term stability (3-year support)
- **Use .NET 9.0 (STS)** if you need the latest features with 24-month support
- Run `dotnet --version` to verify your SDK
- Update regularly: `dotnet workload update`

### ❌ Not Running uno-check

**Problem**: Missing SDKs, workloads, or tools causing mysterious build failures.

**Why it happens**: Skipping environment setup verification after installation.

**Solution**:
```bash
# Install uno-check
dotnet tool install -g uno.check

# Run it before starting development
uno-check

# Update when switching projects or after SDK updates
dotnet tool update -g uno.check
uno-check
```

### ❌ Opening the Full Solution Instead of Solution Filters

**Problem**: Extremely slow builds, IDE hangs, and unnecessary cross-platform compilation.

**Why it happens**: Opening `Uno.UI.sln` instead of platform-specific solution filters.

**Solution**:
- **Always use solution filters**: `Uno.UI-Wasm-only.slnf`, `Uno.UI-Skia-only.slnf`, etc.
- Set up `crosstargeting_override.props` (copy from `.sample` file)
- Choose target framework matching your development focus
- Skia desktop builds fastest for quick iteration

## XAML Mistakes

### ❌ Hard-Coding Colors Instead of Using ThemeResource

**Problem**: UI looks broken in dark mode or doesn't adapt to system themes.

**Why it happens**: Using fixed colors like `Background="White"` instead of theme-aware resources.

**Solution**:
```xml
<!-- ❌ Wrong: Hard-coded colors -->
<Border Background="White">
    <TextBlock Foreground="Black" Text="Hello" />
</Border>

<!-- ✅ Right: Theme-aware resources -->
<Border Background="{ThemeResource SystemControlBackgroundChromeMediumBrush}">
    <TextBlock Foreground="{ThemeResource SystemControlForegroundBaseHighBrush}" 
               Text="Hello" />
</Border>
```

**Common theme resources**:
- `SystemControlBackgroundChromeMediumBrush` - Standard background
- `SystemControlForegroundBaseHighBrush` - Primary text
- `SystemAccentColorBrush` - Accent color

### ❌ Using {Binding} Instead of {x:Bind}

**Problem**: Slower performance, runtime errors that could be caught at compile-time.

**Why it happens**: Using old UWP patterns or copying examples from outdated sources.

**Solution**:
```xml
<!-- ❌ Wrong: Runtime binding -->
<TextBlock Text="{Binding UserName}" />

<!-- ✅ Right: Compile-time binding -->
<TextBlock Text="{x:Bind ViewModel.UserName}" />
<TextBox Text="{x:Bind ViewModel.UserInput, Mode=TwoWay}" />
```

**Benefits of x:Bind**:
- Compile-time validation
- Better performance
- Strongly typed
- IntelliSense support

### ❌ Forgetting x:DataType in DataTemplates

**Problem**: `x:Bind` fails in `DataTemplate` without error message.

**Why it happens**: `x:Bind` requires knowing the data type at compile time.

**Solution**:
```xml
<!-- ❌ Wrong: Missing x:DataType -->
<ListView ItemsSource="{x:Bind Items}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{x:Bind Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>

<!-- ✅ Right: Include x:DataType -->
<ListView ItemsSource="{x:Bind Items}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:Item">
            <TextBlock Text="{x:Bind Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### ❌ Not Using Proper Layout Containers

**Problem**: UI doesn't respond correctly to size changes or looks wrong on different screens.

**Why it happens**: Using absolute positioning or inappropriate containers.

**Solution**:
- Use `StackPanel` for simple linear layouts
- Use `Grid` for complex, responsive layouts
- Use `RelativePanel` for relationship-based positioning
- Avoid fixed `Width`/`Height` - prefer `MinWidth`/`MaxWidth`
- Always test on different screen sizes

## C# and Code-Behind Mistakes

### ❌ Blocking the UI Thread

**Problem**: App becomes unresponsive during long operations.

**Why it happens**: Calling synchronous operations or `.Result` on async methods in UI code.

**Solution**:
```csharp
// ❌ Wrong: Blocking UI thread
public void LoadData()
{
    var data = _service.GetDataAsync().Result; // BLOCKS!
    Items = data;
}

// ✅ Right: Async all the way
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
    Items = data;
}
```

**Key rules**:
- Never use `.Result` or `.Wait()` in UI code
- Use `async`/`await` for all I/O operations
- Use `Task.Run()` for CPU-intensive work
- Update UI on the main thread

### ❌ Not Implementing INotifyPropertyChanged Correctly

**Problem**: UI doesn't update when data changes.

**Why it happens**: Forgetting to raise `PropertyChanged` or implementing it incorrectly.

**Solution**:
```csharp
// ❌ Wrong: No notification
public class MainViewModel
{
    public string Title { get; set; }
}

// ✅ Right: Using ObservableObject base class
public class MainViewModel : ObservableObject
{
    private string _title;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}

// ✅ Alternative: Manual implementation
public class MainViewModel : INotifyPropertyChanged
{
    private string _title;
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
```

### ❌ Mixing Platform-Specific Code Without Conditionals

**Problem**: Code fails to compile or crashes on certain platforms.

**Why it happens**: Using platform APIs without checking availability.

**Solution**:
```csharp
// ❌ Wrong: Unconditional platform code
public void DoSomething()
{
    var result = Android.OS.Build.VERSION.Release; // Fails on iOS!
}

// ✅ Right: Use preprocessor directives
public void DoSomething()
{
#if __ANDROID__
    var result = Android.OS.Build.VERSION.Release;
#elif __IOS__
    var result = UIKit.UIDevice.CurrentDevice.SystemVersion;
#elif __WASM__
    var result = "WebAssembly";
#else
    var result = "Unknown";
#endif
}

// ✅ Alternative: Runtime checks
public void DoSomething()
{
    if (OperatingSystem.IsAndroid())
    {
        // Android-specific code
    }
    else if (OperatingSystem.IsIOS())
    {
        // iOS-specific code
    }
}
```

## Hot Reload Mistakes

### ❌ Hot Reload Not Working

**Problem**: Changes to XAML or C# don't reflect in running app.

**Why it happens**: Missing `UseStudio()` or debugging not attached.

**Solution**:
```csharp
// In App.xaml.cs, ensure UseStudio() is called
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .UseStudio() // Required for Hot Reload
        .Configure(host => host
            .UseNavigation()
            // ... other configuration
        );
    
    MainWindow = builder.Window;
}
```

**Checklist**:
- [ ] `UseStudio()` is called in App.xaml.cs
- [ ] Running in Debug mode
- [ ] Debugger is attached
- [ ] Target framework is .NET 9.0 or .NET 10.0
- [ ] Uno Platform extension installed in IDE

### ❌ Modifying Code That Can't Hot Reload

**Problem**: Some changes require full rebuild despite Hot Reload being enabled.

**Why it happens**: Certain code changes require app restart.

**Solution**:
- **Can Hot Reload**: XAML UI changes, method body changes, adding properties
- **Can't Hot Reload**: Adding new types, changing method signatures, modifying namespaces

**Workaround**: Structure code to minimize signature changes during development.

## Build and Package Mistakes

### ❌ Not Cleaning Before Rebuild

**Problem**: Intermittent build errors, old code executing, mysterious issues.

**Why it happens**: Cached build artifacts conflict with new code.

**Solution**:
```bash
# Quick clean
dotnet clean

# Deep clean (recommended for stubborn issues)
cd src
find . -name "bin" -o -name "obj" | xargs rm -rf

# Then restore and rebuild
dotnet restore
dotnet build
```

**When to clean**:
- Switching branches
- After updating packages
- Unexplained build errors
- Before publishing

### ❌ Forgetting to Update Package Versions Together

**Problem**: Incompatible package versions causing runtime errors.

**Why it happens**: Updating some Uno packages but not others.

**Solution**:
```bash
# Check for outdated packages
dotnet outdated

# Update all Uno packages to same version
dotnet add package Uno.WinUI --version 5.5.x
dotnet add package Uno.WinUI.DevServer --version 5.5.x
dotnet add package Uno.Extensions.Navigation --version 5.5.x
# ... update all related packages
```

**Best practice**: Keep all Uno Platform packages at the same version.

### ❌ Not Testing on All Target Platforms

**Problem**: App works on development platform but fails on others.

**Why it happens**: Platform-specific bugs or missing platform conditionals.

**Solution**:
- Test on Windows, Web (WASM), and at least one mobile platform
- Use solution filters to build for each platform
- Run automated tests for each target
- Use Hot Design<sup>®</sup> Agent to check UI on all platforms

## Performance Mistakes

### ❌ Not Virtualizing Large Lists

**Problem**: Scrolling is laggy with many items, high memory usage.

**Why it happens**: Using `ItemsControl` or `StackPanel` for large collections.

**Solution**:
```xml
<!-- ❌ Wrong: No virtualization -->
<ItemsControl ItemsSource="{x:Bind LargeCollection}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="local:Item">
            <TextBlock Text="{x:Bind Name}" />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- ✅ Right: Virtualized with ListView -->
<ListView ItemsSource="{x:Bind LargeCollection}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:Item">
            <TextBlock Text="{x:Bind Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**Use these for large collections**:
- `ListView` for vertical lists
- `GridView` for grid layouts
- Enable `IsItemClickEnabled` for item selection

### ❌ Loading Large Images Without Optimization

**Problem**: App uses excessive memory, slow load times.

**Why it happens**: Loading full-resolution images when smaller versions would work.

**Solution**:
```xml
<!-- ✅ Set appropriate decode size -->
<Image Source="large-photo.jpg" 
       DecodePixelWidth="300"
       DecodePixelHeight="200" />

<!-- ✅ Lazy load images -->
<ListView ItemsSource="{x:Bind Photos}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:Photo">
            <Image Source="{x:Bind Url}" 
                   Stretch="UniformToFill"
                   DecodePixelWidth="150"
                   IsLazyLoadingEnabled="True" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

## Navigation Mistakes

### ❌ Not Using Uno.Extensions.Navigation

**Problem**: Manual navigation code is complex and error-prone.

**Why it happens**: Not aware of Uno.Extensions.Navigation capabilities.

**Solution**:
```csharp
// ❌ Wrong: Manual frame navigation
var frame = (Frame)Window.Current.Content;
frame.Navigate(typeof(DetailPage), itemId);

// ✅ Right: Use Uno.Extensions.Navigation
await Navigator.NavigateViewModelAsync<DetailViewModel>(this, data: new { ItemId = itemId });
```

**Benefits**:
- Type-safe navigation
- Automatic ViewModel injection
- Back stack management
- Deep linking support

### ❌ Not Handling Navigation Failures

**Problem**: Silent failures when navigation doesn't work.

**Why it happens**: Not checking navigation results or handling errors.

**Solution**:
```csharp
// ✅ Check navigation result
var result = await Navigator.NavigateViewModelAsync<DetailViewModel>(this);
if (!result.Success)
{
    // Handle navigation failure
    await ShowErrorDialogAsync("Navigation failed");
}
```

## Testing Mistakes

### ❌ Not Writing Tests

**Problem**: Regressions go unnoticed, fear of refactoring.

**Why it happens**: Thinking tests are too hard or not worth the effort.

**Solution**:
- Add unit tests for ViewModels and business logic
- Use `Uno.UI.RuntimeTests` for UI testing
- Test on actual devices, not just emulators
- Run tests in CI/CD pipeline

**Example runtime test**:
```csharp
[TestMethod]
public async Task When_Button_Clicked_Text_Changes()
{
    var page = new MainPage();
    await UITestHelper.Load(page);
    
    var button = page.FindName<Button>("MyButton");
    var textBlock = page.FindName<TextBlock>("MyText");
    
    button.RaiseClick();
    
    Assert.AreEqual("Clicked", textBlock.Text);
}
```

## Related Topics

- [Quick Reference Guide](xref:Uno.QuickReference) - Common commands and patterns
- [Troubleshooting Guide](xref:Uno.UI.CommonIssues) - Solutions to common problems
- [Performance Optimization](xref:Uno.UI.Performance) - Make your app faster
- [Testing Guide](xref:Uno.Contributing.Guidelines.CreatingTests) - Write effective tests
- [Hot Design<sup>®</sup> Agent](xref:Uno.HotDesign.Agent) - AI assistant to avoid UI mistakes
