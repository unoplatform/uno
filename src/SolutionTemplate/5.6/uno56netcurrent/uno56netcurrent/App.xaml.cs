using System;
using Microsoft.Extensions.Logging;
using Uno.Resizetizer;

namespace uno56netcurrent;

public partial class App : Application
{
    private readonly bool exitAfterLaunching;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App(bool exitAfterLaunching = false)
    {
        this.exitAfterLaunching = exitAfterLaunching;
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();
#if DEBUG
        MainWindow.UseStudio();
#endif


        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        MainWindow.SetWindowIcon();
        // Ensure the current window is active
        MainWindow.Activate();

        if (exitAfterLaunching)
        {
            await Task.Delay(1000);
            Exit();
        }
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
        // Logging must be enabled for all build configurations so that `.AddFakeLogging()` can be used to
        // catch binding errors in test and production builds.
        //
        // Performance is not a concern here.  This is an integration test.

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            builder.AddFakeLogging(options =>
            {
                options.FilteredCategories.Add("Uno.UI.DataBinding.BindingPropertyHelper");
                options.OutputSink = message =>
                {
                    if (message.Contains("property getter does not exist on type", StringComparison.Ordinal))
                    {
                        // We have a `{Binding …}` expression which refers to a non-existent member,
                        // or the relevant `DataContext` is not properly set at the time of the binding evaluation,
                        // or `Uno.Bindable.Descriptor.xml` is missing an entry for the type and property mentioned.
                        //
                        // For example, if `message` contains:
                        //
                        //  The [HelloText] property getter does not exist on type [unoapp.MainPage]
                        //
                        // *If* `unoapp.MainPage.HelloText` exists (or should exist), then
                        // `Uno.Bindable.Descriptor.xml` *should* contain an entry like:
                        //
                        //   <type fullname="unoapp.MainPage">
                        //     <property name="HelloText" />
                        //   </type>
                        //
                        // If it doesn't, that's a bug; please file a repro.
                        //
                        // Use `Environment.FailFast()` to (1) ensure the failure is visible on CI
                        // with a non-zero exit code, and (2) *if anyone is debugging the app*,
                        // allow the debugger to "break" on the failure before app exit.
                        Environment.FailFast(message);
                    }
                };
            });

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
    }
}
