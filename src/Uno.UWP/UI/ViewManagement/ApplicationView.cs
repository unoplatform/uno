#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Storage;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using MUXWindowId = Microsoft.UI.WindowId;

namespace Windows.UI.ViewManagement
{
	public partial class ApplicationView
		: IApplicationViewSpanningRects
	{
		private const string PreferredLaunchViewWidthKey = "__Uno.PreferredLaunchViewSizeKey.Width";
		private const string PreferredLaunchViewHeightKey = "__Uno.PreferredLaunchViewSizeKey.Height";

		private static readonly ConcurrentDictionary<MUXWindowId, ApplicationView> _windowIdMap = new();

		private readonly MUXWindowId _windowId;

		private ApplicationViewTitleBar _titleBar = new ApplicationViewTitleBar();
		private IReadOnlyList<Rect> _defaultSpanningRects;
		private IApplicationViewSpanningRects _applicationViewSpanningRects;

		private Rect _visibleBounds;

		[global::Uno.NotImplemented]
		public int Id => 1;

		internal ApplicationView(MUXWindowId windowId)
		{
			_windowId = windowId;
			InitializePlatform();
		}

		partial void InitializePlatform();

		public string Title
		{
			get => AppWindow.GetFromWindowId(_windowId).Title;
			set => AppWindow.GetFromWindowId(_windowId).Title = value;
		}

		public ApplicationViewOrientation Orientation
		{
			get
			{
				if (VisibleBounds.Height > VisibleBounds.Width)
				{
					return ApplicationViewOrientation.Portrait;
				}
				else
				{
					return ApplicationViewOrientation.Landscape;
				}
			}
		}

		public ApplicationViewBoundsMode DesiredBoundsMode { get; private set; }

		public bool SetDesiredBoundsMode(global::Windows.UI.ViewManagement.ApplicationViewBoundsMode boundsMode)
		{
			DesiredBoundsMode = boundsMode;

			return true;
		}

		public Foundation.Rect VisibleBounds => _visibleBounds;

		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.ApplicationView, object> VisibleBoundsChanged;

		/// <summary>
		/// Gets a value that indicates whether the app is running in full-screen mode.
		/// </summary>
		public bool IsFullScreenMode => GetAppWindow()?.Presenter is FullScreenPresenter;

		public bool TryEnterFullScreenMode()
		{
			if (GetAppWindow() is { } appWindow)
			{
				appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
				return appWindow.Presenter is FullScreenPresenter;
			}

			return false;
		}

		public void ExitFullScreenMode() => GetAppWindow()?.SetPresenter(AppWindowPresenterKind.Default);

		public global::Windows.UI.ViewManagement.ApplicationViewTitleBar TitleBar => _titleBar;

		public static global::Windows.UI.ViewManagement.ApplicationView GetForCurrentView()
		{
			// This is needed to ensure for "current view" there is always a corresponding ApplicationView instance.
			// This means that Uno Islands and WinUI apps can keep using this API for now until we make the breaking change
			// on Uno.WinUI codebase.
			return GetOrCreateForWindowId(AppWindow.MainWindowId);
		}

		private AppWindow GetAppWindow() => AppWindow.GetFromWindowId(_windowId);

#pragma warning disable RS0030 // Do not use banned APIs
		public static global::Windows.UI.ViewManagement.ApplicationView GetForCurrentViewSafe() => GetForCurrentView();
#pragma warning restore RS0030 // Do not use banned APIs

		internal static global::Windows.UI.ViewManagement.ApplicationView GetForWindowId(MUXWindowId windowId)
		{
			if (!_windowIdMap.TryGetValue(windowId, out var appView))
			{
				throw new InvalidOperationException(
					$"ApplicationView corresponding with this window does not exist yet, which usually means " +
					$"the API was called too early in the windowing lifecycle. Try to use ApplicationView later.");
			}

			return appView;
		}

		internal MUXWindowId WindowId => _windowId;

		internal static ApplicationView GetOrCreateForWindowId(MUXWindowId windowId)
		{
			if (!_windowIdMap.TryGetValue(windowId, out var appView))
			{
				appView = new(windowId);
				_windowIdMap[windowId] = appView;
			}

			return appView;
		}

		[global::Uno.NotImplemented]
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.ApplicationView, global::Windows.UI.ViewManagement.ApplicationViewConsolidatedEventArgs> Consolidated;

		public ApplicationViewMode ViewMode
		{
			get
			{
				TryInitializeSpanningRectsExtension();

				return (_applicationViewSpanningRects as INativeDualScreenProvider)?.IsSpanned == true
					? ApplicationViewMode.Spanning
					: ApplicationViewMode.Default;
			}
		}

		public static Size PreferredLaunchViewSize
		{
			get
			{
				if (ApplicationData.Current.LocalSettings.Values.TryGetValue(PreferredLaunchViewWidthKey, out var widthObject) &&
					ApplicationData.Current.LocalSettings.Values.TryGetValue(PreferredLaunchViewHeightKey, out var heightObject) &&
					widthObject is double width &&
					heightObject is double height)
				{
					return new Size(width, height);
				}
				return Size.Empty;
			}

			set
			{
				double width = value.Width;
				double height = value.Height;

				ApplicationData.Current.LocalSettings.Values[PreferredLaunchViewWidthKey] = width;
				ApplicationData.Current.LocalSettings.Values[PreferredLaunchViewHeightKey] = height;
			}
		}

		public bool IsViewModeSupported(ApplicationViewMode viewMode)
		{
			if (viewMode == ApplicationViewMode.Default)
			{
				return true;
			}
			else if (viewMode == ApplicationViewMode.Spanning)
			{
				return (_applicationViewSpanningRects as INativeDualScreenProvider)?.SupportsSpanning == true;
			}

			return false;
		}

		public IAsyncOperation<bool> TryEnterViewModeAsync(global::Windows.UI.ViewManagement.ApplicationViewMode viewMode) =>
			AsyncOperation.FromTask(cancellation =>
			{
				if (ViewMode == viewMode)
				{
					// If we are already in the requested mode, we can return true.
					return Task.FromResult(true);
				}

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().LogWarning(
						$"Cannot not enter view mode {viewMode}, " +
						$"as this transition is not yet supported.");
				}

				return Task.FromResult(false);
			});

		public IAsyncOperation<bool> TryEnterViewModeAsync(global::Windows.UI.ViewManagement.ApplicationViewMode viewMode, global::Windows.UI.ViewManagement.ViewModePreferences viewModePreferences) =>
				TryEnterViewModeAsync(viewMode);

		public IReadOnlyList<Rect> GetSpanningRects()
		{
			TryInitializeSpanningRectsExtension();

			return _applicationViewSpanningRects?.GetSpanningRects() ?? _defaultSpanningRects;
		}

		private void TryInitializeSpanningRectsExtension()
		{
			if (_defaultSpanningRects == null && _applicationViewSpanningRects == null)
			{
				if (!ApiExtensibility.CreateInstance<IApplicationViewSpanningRects>(this, out _applicationViewSpanningRects))
				{
					_defaultSpanningRects = new List<Rect>(0);
				}
			}
		}

		internal void SetVisibleBounds(Rect visibleBounds)
		{
			if (_visibleBounds != visibleBounds)
			{
				_visibleBounds = visibleBounds;
				VisibleBoundsChanged?.Invoke(this, null);
			}
		}
	}
}
