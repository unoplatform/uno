using System;
using System.IO;
using Gtk;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Extensions.UI.Core.Preview;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Core.Preview;
using Uno.UI.Runtime.Skia.GTK.Extensions.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.GTK.Extensions.Helpers;
using Uno.UI.Runtime.Skia.GTK.Extensions.Helpers.Theming;
using Uno.UI.Runtime.Skia.GTK.Extensions.System;
using Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.GTK.System.Profile;
using Uno.UI.Runtime.Skia.GTK.UI.Core;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using UnoApplication = Windows.UI.Xaml.Application;
using WUX = Windows.UI.Xaml;
using Uno.UI.Runtime.Skia.GTK.System.Profile;
using Uno.UI.Runtime.Skia.Helpers;
using Uno.UI.Runtime.Skia.Helpers.Dpi;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia
{
	public class GtkHost : ISkiaHost
	{
		private const int UnoThemePriority = 800;

		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private readonly Func<WUX.Application> _appBuilder;
		private static Gtk.Window _window;
		private static Gtk.EventBox _eventBox;
		private Widget _area;
		private Fixed _fix;
		private GtkDisplayInformationExtension _displayInformationExtension;

		public static Gtk.Window Window => _window;
		public static Gtk.EventBox EventBox => _eventBox;

		/// <summary>
		/// Gets or sets the current Skia Render surface type.
		/// </summary>
		/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
		public RenderSurfaceType? RenderSurfaceType { get; set; }

		/// <summary>
		/// Creates a host for a Uno Skia GTK application.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <param name="args">Deprecated, value ignored.</param>
		/// <remarks>
		/// Args are obsolete and will be removed in the future. Environment.CommandLine is used instead
		/// to fill LaunchEventArgs.Arguments.
		/// </remarks>
		public GtkHost(Func<WUX.Application> appBuilder, string[] args)
		{
			_appBuilder = appBuilder;
		}

		public void Run()
		{
			Gtk.Application.Init();
			SetupTheme();

			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new GtkCoreWindowExtension(o));
			ApiExtensibility.Register<Windows.UI.Xaml.Application>(typeof(Uno.UI.Xaml.IApplicationExtension), o => new GtkApplicationExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new GtkApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new GtkSystemThemeHelperExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => _displayInformationExtension ??= new GtkDisplayInformationExtension(o, _window));
			ApiExtensibility.Register<TextBoxView>(typeof(ITextBoxViewExtension), o => new TextBoxViewExtension(o, _window));
			ApiExtensibility.Register(typeof(ILauncherExtension), o => new LauncherExtension(o));
			ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
			ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new FolderPickerExtension(o));
			ApiExtensibility.Register(typeof(IClipboardExtension), o => new ClipboardExtensions(o));
			ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
			ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new AnalyticsInfoExtension());
			ApiExtensibility.Register(typeof(ISystemNavigationManagerPreviewExtension), o => new SystemNavigationManagerPreviewExtension(_window));

			_isDispatcherThread = true;
			_window = new Gtk.Window("Uno Host");
			Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
			if (preferredWindowSize != Size.Empty)
			{
				_window.SetDefaultSize((int)preferredWindowSize.Width, (int)preferredWindowSize.Height);
			}
			else
			{
				_window.SetDefaultSize(1024, 800);
			}
			_window.SetPosition(Gtk.WindowPosition.Center);

			_window.Realized += (s, e) =>
			{
				// Load the correct cursors before the window is shown
				// but after the window has been initialized.
				Cursors.Reload();
			};

			_window.DeleteEvent += WindowClosing;

			void Dispatch(System.Action d)
			{
				if (Gtk.Application.EventsPending())
				{
					Gtk.Application.RunIteration(false);
				}

				GLib.Idle.Add(delegate
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Iteration");
					}

					try
					{
						d();
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}

					return false;
				});
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			_window.WindowStateEvent += OnWindowStateChanged;

			var overlay = new Overlay();

			_eventBox = new EventBox();
			_area = BuildRenderSurfaceType();
			_fix = new Fixed();
			overlay.Add(_area);
			overlay.AddOverlay(_fix);
			_eventBox.Add(overlay);
			_window.Add(_eventBox);

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Using {RenderSurfaceType} rendering");
			}

			_area.Realized += (s, e) =>
			{
				WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(_area.AllocatedWidth, _area.AllocatedHeight));
			};

			_area.SizeAllocated += (s, e) =>
			{
				WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(e.Allocation.Width, e.Allocation.Height));
			};

			/* avoids double invokes at window level */
			_area.AddEvents((int)GtkCoreWindowExtension.RequestedEvents);

			_window.ShowAll();

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			WUX.Application.StartWithArguments(CreateApp);

			UpdateWindowPropertiesFromPackage();

			Gtk.Application.Run();
		}

		private void WindowClosing(object sender, DeleteEventArgs args)
		{
			var manager = SystemNavigationManagerPreview.GetForCurrentView();
			if (!manager.HasConfirmedClose)
			{
				if (!manager.RequestAppClose())
				{
					// App closing was prevented, handle event
					args.RetVal = true;
					return;
				}
			}

			// Closing should continue, perform suspension.
			UnoApplication.Current.RaiseSuspending();

			// All prerequisites passed, can safely close.
			args.RetVal = false;
			Gtk.Main.Quit();
		}

		private Widget BuildRenderSurfaceType()
		{
			if(RenderSurfaceType == null)
			{
				if (OpenGLESRenderSurface.IsSupported)
				{
					RenderSurfaceType = Skia.RenderSurfaceType.OpenGLES;
				}
				else if (OpenGLRenderSurface.IsSupported)
				{
					RenderSurfaceType = Skia.RenderSurfaceType.OpenGL;
				}
				else
				{
					RenderSurfaceType = Skia.RenderSurfaceType.Software;
				}
			}

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().LogInfo($"Using {RenderSurfaceType} render surface");
			}

			return RenderSurfaceType switch
			{
				Skia.RenderSurfaceType.OpenGLES => new OpenGLESRenderSurface(),
				Skia.RenderSurfaceType.OpenGL => new OpenGLRenderSurface(),
				Skia.RenderSurfaceType.Software => new SoftwareRenderSurface(),
				_ => throw new InvalidOperationException($"Unsupported RenderSurfaceType {RenderSurfaceType}")
			};
		}

		private void OnWindowStateChanged(object o, WindowStateEventArgs args)
		{
			var winUIApplication = WUX.Application.Current;
			var winUIWindow = WUX.Window.Current;
			var newState = args.Event.NewWindowState;
			var changedState = args.Event.ChangedMask;

			var isVisible =
				!(newState.HasFlag(Gdk.WindowState.Withdrawn) ||
				newState.HasFlag(Gdk.WindowState.Iconified));

			var isVisibleChanged =
				changedState.HasFlag(Gdk.WindowState.Withdrawn) ||
				changedState.HasFlag(Gdk.WindowState.Iconified);

			var focused = newState.HasFlag(Gdk.WindowState.Focused);
			var focusChanged = changedState.HasFlag(Gdk.WindowState.Focused);

			if (!focused && focusChanged)
			{
				winUIWindow?.OnActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);
			}

			if (isVisibleChanged)
			{
				if (isVisible)
				{
					winUIApplication?.RaiseLeavingBackground(() => winUIWindow?.OnVisibilityChanged(true));					
				}
				else
				{
					winUIWindow?.OnVisibilityChanged(false);
					winUIApplication?.RaiseEnteredBackground(null);
				}
			}

			if (focused && focusChanged)
			{
				winUIWindow?.OnActivated(Windows.UI.Core.CoreWindowActivationState.CodeActivated);
			}
		}

		private void UpdateWindowPropertiesFromPackage()
		{
			if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
			{
				var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
				var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, basePath);

				if (File.Exists(iconPath))
				{
					if (this.Log().IsEnabled(LogLevel.Information))
					{
						this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
					}

					GtkHost.Window.SetIconFromFile(iconPath);
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
					}
				}
			}

			Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}

		public void TakeScreenshot(string filePath)
		{
			if (_area is IRenderSurface renderSurface)
			{
				renderSurface.TakeScreenshot(filePath);
			}
		}

		private void SetupTheme()
		{
			var cssProvider = new CssProvider();
			cssProvider.LoadFromEmbeddedResource("Theming.UnoGtk.css");
			StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, UnoThemePriority);
		}
	}
}
