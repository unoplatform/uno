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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnoApplication = Microsoft.UI.Xaml.Application;
using WUX = Microsoft.UI.Xaml;
using Uno.UI.Xaml.Core;
using System.ComponentModel;
using Uno.Disposables;
using System.Collections.Generic;
using Uno.Extensions.ApplicationModel.Core;
using Windows.ApplicationModel;

namespace Uno.UI.Runtime.Skia
{
	public class GtkHost : ISkiaHost
	{
		private const int UnoThemePriority = 800;

		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private readonly Func<WUX.Application> _appBuilder;
		private IRenderSurface _renderSurface;
		private static Gtk.Window _window;
		private static UnoEventBox _eventBox;
		private Widget _area;
		private Fixed _fix;
		private GtkDisplayInformationExtension _displayInformationExtension;
		private CompositeDisposable _registrations = new();

		private record PendingWindowStateChangedInfo(Gdk.WindowState newState, Gdk.WindowState changedMask);
		private List<PendingWindowStateChangedInfo> _pendingWindowStateChanged = new();

		public static Gtk.Window Window => _window;
		internal static UnoEventBox EventBox => _eventBox;

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
		[EditorBrowsable(EditorBrowsableState.Never)]
		public GtkHost(Func<WUX.Application> appBuilder, string[] args) : this(appBuilder)
		{
		}

		public GtkHost(Func<WUX.Application> appBuilder)
		{
			_appBuilder = appBuilder;
		}

		public void Run()
		{
			Microsoft.UI.Xaml.Documents.Inline.ApplyHarfbuzzWorkaround();

			if (!InitializeGtk())
			{
				return;
			}

			SetupTheme();

			ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => new CoreApplicationExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new GtkCoreWindowExtension(o));
			ApiExtensibility.Register<Microsoft.UI.Xaml.Application>(typeof(Uno.UI.Xaml.IApplicationExtension), o => new GtkApplicationExtension(o));
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
			_window = new Gtk.Window("GTK Host");
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

			Windows.UI.Core.CoreDispatcher.DispatchOverride = DispatchNativeSingle;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			_window.WindowStateEvent += OnWindowStateChanged;
			_window.ShowAll();

			SetupRenderSurface();

			Gtk.Application.Run();
		}

		private bool InitializeGtk()
		{
			try
			{
				Gtk.Application.Init();
				return true;
			}
			catch (TypeInitializationException e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Unable to initialize Gtk, visit https://aka.platform.uno/gtk-install for more information.", e);
				}
				return false;
			}
		}

		private void DispatchNativeSingle(System.Action d)
			=> GLib.Idle.Add(delegate
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Dispatch Iteration");
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

		private void SetupRenderSurface()
		{
			TryReadRenderSurfaceTypeEnvironment();

			if (!OpenGLRenderSurface.IsSupported && !OpenGLESRenderSurface.IsSupported)
			{
				// Pre-validation is required to avoid initializing OpenGL on macOS
				// where the whole app may get visually corrupted even if OpenGL is not
				// used in the app.

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Neither OpenGL or OpenGL ES are supporting, using software rendering");
				}

				RenderSurfaceType = Skia.RenderSurfaceType.Software;
			}

			if (RenderSurfaceType == null)
			{
				// Create a temporary surface to automatically detect
				// the OpenGL environment that can be used on the system.
				GLValidationSurface validationSurface = new();

				_window.Add(validationSurface);
				_window.ShowAll();

				DispatchNativeSingle(ValidatedSurface);

				async void ValidatedSurface()
				{
					try
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"Auto-detecting surface type");
						}

						// Wait for a realization of the GLValidationSurface
						RenderSurfaceType = await validationSurface.GetSurfaceTypeAsync();

						// Continue on the GTK main thread
						DispatchNativeSingle(() =>
						{
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().Debug($"Auto-detected {RenderSurfaceType} rendering");
							}

							_window.Remove(validationSurface);

							FinalizeStartup();
						});
					}
					catch (Exception e)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"Auto-detected failed", e);
						}
					}
				}
			}
			else
			{
				FinalizeStartup();
			}
		}

		private void FinalizeStartup()
		{
			var overlay = new Overlay();

			_eventBox = new UnoEventBox();

			_renderSurface = BuildRenderSurfaceType();
			_area = (Widget)_renderSurface;
			_fix = new Fixed();
			overlay.Add(_area);
			overlay.AddOverlay(_fix);
			_eventBox.Add(overlay);
			_window.Add(_eventBox);

			// Show the whole tree again, since we may have
			// swapped the content with the GLValidationSurface.
			_window.ShowAll();

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

			ReplayPendingWindowStateChanges();

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

			WUX.Application.StartWithArguments(CreateApp);
			UpdateWindowPropertiesFromPackage();

			RegisterForBackgroundColor();
		}

		private void TryReadRenderSurfaceTypeEnvironment()
		{
			if (Enum.TryParse(Environment.GetEnvironmentVariable("UNO_RENDER_SURFACE_TYPE"), out RenderSurfaceType surfaceType))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Overriding RnderSurfaceType using command line with {surfaceType}");
				}

				RenderSurfaceType = surfaceType;
			}
		}
		private void RegisterForBackgroundColor()
		{
			if (_area is IRenderSurface renderSurface)
			{
				void Update()
				{
					if (WUX.Window.Current.Background is WUX.Media.SolidColorBrush brush)
					{
						renderSurface.BackgroundColor = brush.Color;
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().Warn($"This platform only supports SolidColorBrush for the Window background");
						}
					}

				}

				Update();

				_registrations.Add(WUX.Window.Current.RegisterBackgroundChangedEvent((s, e) => Update()));
			}
		}

		private void OnCoreWindowContentRootSet(object sender, object e)
		{
			var xamlRoot = CoreServices.Instance
				.ContentRootCoordinator
				.CoreWindowContentRoot?
				.GetOrCreateXamlRoot();

			if (xamlRoot is null)
			{
				throw new InvalidOperationException("XamlRoot was not properly initialized");
			}

			xamlRoot.InvalidateRender += _renderSurface.InvalidateRender;

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
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

		private IRenderSurface BuildRenderSurfaceType()
			=> RenderSurfaceType switch
			{
				Skia.RenderSurfaceType.OpenGLES => new OpenGLESRenderSurface(),
				Skia.RenderSurfaceType.OpenGL => new OpenGLRenderSurface(),
				Skia.RenderSurfaceType.Software => new SoftwareRenderSurface(),
				_ => throw new InvalidOperationException($"Unsupported RenderSurfaceType {RenderSurfaceType}")
			};

		private void OnWindowStateChanged(object o, WindowStateEventArgs args)
		{
			var newState = args.Event.NewWindowState;
			var changedMask = args.Event.ChangedMask;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"OnWindowStateChanged: {newState}/{changedMask}");
			}

			if (_area != null)
			{
				ProcessWindowStateChanged(newState, changedMask);
			}
			else
			{
				// Store state changes to replay once the application has been
				// initalized completely (initialization can be delayed if the render
				// surface is automatically detected).
				_pendingWindowStateChanged?.Add(new(newState, changedMask));
			}
		}

		private void ReplayPendingWindowStateChanges()
		{
			if (_pendingWindowStateChanged is not null)
			{
				foreach (var state in _pendingWindowStateChanged)
				{
					ProcessWindowStateChanged(state.newState, state.changedMask);
				}

				_pendingWindowStateChanged = null;
			}
		}

		private static void ProcessWindowStateChanged(Gdk.WindowState newState, Gdk.WindowState changedMask)
		{
			var winUIApplication = WUX.Application.Current;
			var winUIWindow = WUX.Window.Current;

			var isVisible =
				!(newState.HasFlag(Gdk.WindowState.Withdrawn) ||
				newState.HasFlag(Gdk.WindowState.Iconified));

			var isVisibleChanged =
				changedMask.HasFlag(Gdk.WindowState.Withdrawn) ||
				changedMask.HasFlag(Gdk.WindowState.Iconified);

			var focused = newState.HasFlag(Gdk.WindowState.Focused);
			var focusChanged = changedMask.HasFlag(Gdk.WindowState.Focused);

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
				var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

				if (File.Exists(iconPath))
				{
					if (this.Log().IsEnabled(LogLevel.Information))
					{
						this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
					}

					GtkHost.Window.SetIconFromFile(iconPath);
				}
				else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
				{
					if (this.Log().IsEnabled(LogLevel.Information))
					{
						this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
					}

					GtkHost.Window.SetIconFromFile(scaledPath);
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
