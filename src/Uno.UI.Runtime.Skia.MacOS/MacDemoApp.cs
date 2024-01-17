// TODO: remove - for testing purpose

#nullable enable

using System;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Runtime.Skia.MacOS;
internal class AppHead : Application {

	Window? _window;

	public AppHead()
	{
		// testing setting custom window title
		ApplicationView.GetForCurrentView().Title = "Gtk-less macOS Uno Host";
		// testing app icon support
		Windows.ApplicationModel.Package.Current.Logo = new Uri("/Users/poupou/git/external/uno/uno/src/SamplesApp/SamplesApp.Shared/Assets/square44x44logo.scale-200.png");
	}

	protected internal override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
	{
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
		_window = new Window();
#else
		_window = Microsoft.UI.Xaml.Window.Current;
#endif

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (_window.Content is not Frame rootFrame)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new Frame();
	
			// Place the frame in the current Window
			_window.Content = rootFrame;

			rootFrame.NavigationFailed += OnNavigationFailed;
		}

		if (rootFrame.Content == null)
		{
			// When the navigation stack isn't restored navigate to the first page,
			// configuring the new page by passing required information as a navigation
			// parameter
			rootFrame.Navigate(typeof(MainPage), args.Arguments);
		}

		// Ensure the current window is active
		_window.Activate();
	}

	void OnNavigationFailed(object sender, Microsoft.UI.Xaml.Navigation.NavigationFailedEventArgs e)
	{
		throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
	}

	private static void Main()
	{
		AppDomain.CurrentDomain.UnhandledException += delegate(object sender, System.UnhandledExceptionEventArgs args)
		{
			Console.WriteLine($"UNHANDLED {(args.IsTerminating ? "FINAL" : "")} EXCEPTION {args.ExceptionObject}");
		};

		var host = new MacSkiaHost(() => new AppHead());
		host.Run();
	}
}

public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
{
	UISettings settings = new();

	public MainPage()
	{
		this.InitializeComponent();
		Loaded += delegate
		{
			Focus(FocusState.Programmatic);
		};

		SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += CloseRequested;

		settings.ColorValuesChanged += ColorValuesChanged;
	}

	private void CloseRequested(object? sender, SystemNavigationCloseRequestedPreviewEventArgs args)
	{
		Console.WriteLine("SystemNavigationManagerPreview.CloseRequested");
	}

	private void ColorValuesChanged(UISettings sender, object args)
	{
		var bg = settings.GetColorValue(UIColorType.Background);
		Background = bg == Colors.Black ? Windows.UI.Colors.DarkCyan : Windows.UI.Colors.Cyan;
	}

	public void InitializeComponent()
	{
		ColorValuesChanged(settings, null!);
		var tb = new Microsoft.UI.Xaml.Controls.TextBlock
		{
			Text = "Bonjour ♥️ Uno!",
			FontSize = 96,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		var b = new Button();
		b.AddChild(tb);
		var g = new Grid();
		g.RowDefinitions.Add (new RowDefinition() { Height = GridLength.Auto });
		g.VerticalAlignment = VerticalAlignment.Center;
		g.KeyDown += TestKeyDown;
		g.KeyUp += TestKeyUp;
		g.Children.Add(b);
		Content = g;
	}

	void TestKeyDown(object sender, KeyRoutedEventArgs e)
	{
		// assume it's handled (so it won't _beep_)
		e.Handled = true;
		switch (e.Key) {
			case Windows.System.VirtualKey.A:
			{
				// Test for IFolderPickerExtension
				// status: works
				var picker = new Windows.Storage.Pickers.FolderPicker();
				var a = picker.PickSingleFolderAsync();
				Console.WriteLine(a.GetAwaiter().GetResult()?.Name);
				break;
			}
			case Windows.System.VirtualKey.B:
			{
				// Test for IFileOpenPickerExtension
				var picker = new Windows.Storage.Pickers.FileOpenPicker();
				picker.FileTypeFilter.Add("*");
				
				if (e.KeyboardModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift))
				{
					// status: works
					var a = picker.PickMultipleFilesAsync();
					foreach (var item in a.GetAwaiter().GetResult())
					{
						Console.WriteLine(item.Name);
					}
				}
				else
				{
					// status: works
					var a = picker.PickSingleFileAsync();
					Console.WriteLine(a.GetAwaiter().GetResult()?.Name);
				}
				break;
			}
			case Windows.System.VirtualKey.C:
			{
				// Test for IFileSavePickerExtension
				// status: works
				var picker = new Windows.Storage.Pickers.FileSavePicker();
				var a = picker.PickSaveFileAsync();
				Console.WriteLine(a.GetAwaiter().GetResult()?.Name);
				break;
			}
			case Windows.System.VirtualKey.D:
			{
				// Test for IApplicationViewExtension.TryEnterFullScreenMode
				// status: works
				var v = ApplicationView.GetForCurrentView();
				// Console.WriteLine($"IsFullScreen: {v.IsFullScreen}"); throws NotImplementedException
				var result = v.TryEnterFullScreenMode();
				Console.WriteLine($"TryEnterFullScreenMode: {result}");
				// Console.WriteLine($"IsFullScreen: {v.IsFullScreen}"); throws NotImplementedException
				result = v.TryEnterFullScreenMode();
				Console.WriteLine($"TryEnterFullScreenMode (again): {result}");
				// Console.WriteLine($"IsFullScreen: {v.IsFullScreen}"); throws NotImplementedException
				break;
			}
			case Windows.System.VirtualKey.E:
			{
				// Test for IApplicationViewExtension.ExitFullScreenMode
				// status: works
				var v = ApplicationView.GetForCurrentView();
				// Console.WriteLine($"IsFullScreen: {v.IsFullScreen}"); throws NotImplementedException
				v.ExitFullScreenMode();
				// Console.WriteLine($"IsFullScreen: {v.IsFullScreen}"); throws NotImplementedException
				break;
			}
			case Windows.System.VirtualKey.F:
			{
				// Test for IApplicationViewExtension.TryResizeView
				// status: works
				ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(800, 600));
				break;
			}
			case Windows.System.VirtualKey.G:
			{
				// Test for Handled = false -> this will beep (macOS), since it's unhandled
				// status: works
				e.Handled = false;
				Console.WriteLine($"Handled: {e.Handled} - this will BEEP");
				break;
			}
			case Windows.System.VirtualKey.H:
			{
				// Test for Handled = true -> this will NOT beep (macOS), since it's handled
				// status: works
				e.Handled = true;
				Console.WriteLine($"Handled: {e.Handled} - this will not beep");
				break;
			}
			case Windows.System.VirtualKey.I:
			{
				// Test for IUnoKeyboardInputSource.KeyDown
				// status: works
				Console.WriteLine($"KeyDown: {e.Key} modifier: {e.KeyboardModifiers}");
				// e.Handled = false;
				break;
			}
			case Windows.System.VirtualKey.J:
			{
				// Test for IAnalyticsInfoExtension
				// status: works
				Console.WriteLine($"Windows.System.Profile.AnalyticsInfo.DeviceForm: {Windows.System.Profile.AnalyticsInfo.DeviceForm}");
				break;
			}
			case Windows.System.VirtualKey.K:
			{
				// Test for ILauncherExtension.QueryUriSupportAsync
				// status: works
				var uri = Windows.ApplicationModel.Package.Current.Logo;
				var l = Launcher.QueryUriSupportAsync(uri, LaunchQuerySupportType.Uri);
				var result = l.GetAwaiter().GetResult();
				Console.WriteLine($"Querying {uri} -> {result}");
				break;
			}
			case Windows.System.VirtualKey.L:
			{
				// Test for ILauncherExtension.LaunchUriAsync
				// status: works
				var uri = Windows.ApplicationModel.Package.Current.Logo;
				Console.WriteLine($"Opening {uri}");
				var l = Launcher.LaunchUriAsync(uri);
				l.GetAwaiter().GetResult();
				break;
			}
			case Windows.System.VirtualKey.X:
			{
				// Test for `ICoreApplicationHost.Exit`, other parts can be tested with `Cmd+Q`
				// status: works
				Console.WriteLine("Application.Current.Exit");
				Application.Current.Exit();
				break;
			}
			case Windows.System.VirtualKey.Number0:
			{
				Console.WriteLine($"Hiding PointerCursor");
				// documented as such in https://learn.microsoft.com/en-us/windows/uwp/gaming/relative-mouse-movement
				// put the API is not a nullable type ?!?
				Window.Current.CoreWindow.PointerCursor = null!;
				break;
			}
			case Windows.System.VirtualKey.Number1:
			case Windows.System.VirtualKey.Number2:
			case Windows.System.VirtualKey.Number3:
			case Windows.System.VirtualKey.Number4:
			case Windows.System.VirtualKey.Number5:
			case Windows.System.VirtualKey.Number6:
			case Windows.System.VirtualKey.Number7:
			case Windows.System.VirtualKey.Number8:
			case Windows.System.VirtualKey.Number9:
			{
				// Test for IUnoCorePointerInputSource.PointerCursor
				// status: works (but not all cursors are available from macOS)
				var type = (CoreCursorType)(e.Key - Windows.System.VirtualKey.Number1);
				// CoreCursorType goes up to 15...
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					type += 10;
				}
				Console.WriteLine($"PointerCursor = {type}");
				Window.Current.CoreWindow.PointerCursor = new CoreCursor(type, 0);
				break;
			}
			case Windows.System.VirtualKey.F1:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerEntered -= MouseEntered;
				}
				else
				{
					Window.Current.CoreWindow.PointerEntered += MouseEntered;
				}
				break;
			}
			case Windows.System.VirtualKey.F2:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerExited -= MouseExited;
				}
				else
				{
					Window.Current.CoreWindow.PointerExited += MouseExited;
				}
				break;
			}
			case Windows.System.VirtualKey.F3:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerPressed -= MousePressed;
				}
				else
				{
					Window.Current.CoreWindow.PointerPressed += MousePressed;
				}
				break;
			}
			case Windows.System.VirtualKey.F4:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerReleased -= MouseReleased;
				}
				else
				{
					Window.Current.CoreWindow.PointerReleased += MouseReleased;
				}
				break;
			}
			case Windows.System.VirtualKey.F5:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerMoved -= MouseMoved;
				}
				else
				{
					Window.Current.CoreWindow.PointerMoved += MouseMoved;
				}
				break;
			}
			case Windows.System.VirtualKey.F6:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerWheelChanged -= MouseWheelChanged;
				}
				else
				{
					Window.Current.CoreWindow.PointerWheelChanged += MouseWheelChanged;
				}
				break;
			}
			case Windows.System.VirtualKey.F7:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerCaptureLost -= MouseCaptureLost;
				}
				else
				{
					Window.Current.CoreWindow.PointerCaptureLost += MouseCaptureLost;
				}
				break;
			}
			case Windows.System.VirtualKey.F8:
			{
				if (e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					Window.Current.CoreWindow.PointerCancelled -= MouseCancelled;
				}
				else
				{
					Window.Current.CoreWindow.PointerCancelled += MouseCancelled;
				}
				break;
			}
			default:
			{
				// unhandled keys will _beep_
				e.Handled = false;
				break;
			}
		}
	}

	void MouseEntered(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MouseEntered {args.ToString()}");
	}

	void MouseExited(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MouseExited {args.ToString()}");
	}

	void MousePressed(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MousePressed {args.ToString()}");
	}
	
	void MouseReleased(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MouseReleased {args.ToString()}");
	}

	void MouseMoved(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MouseMoved {args.ToString()}");
	}

	void MouseWheelChanged(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MouseWheelChanged {args.ToString()}");
	}

	void MouseCaptureLost(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MouseCaptureLost {args.ToString()}");
	}

	void MouseCancelled(CoreWindow window, PointerEventArgs args)
	{
		Console.WriteLine($"MouseCancelled {args.ToString()}");
	}

	void TestKeyUp(object sender, KeyRoutedEventArgs e)
	{
		switch (e.Key) {
			case Windows.System.VirtualKey.I:
			{
				// Test for IUnoKeyboardInputSource.KeyUp
				// status: works
				Console.WriteLine($"KeyUp: {e.Key} modifier: {e.KeyboardModifiers}");
				break;
			}
		}
	}
}
