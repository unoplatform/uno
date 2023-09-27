#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.WindowManagement;

namespace Windows.UI.ViewManagement
{
	public partial class ApplicationView
		: IApplicationViewSpanningRects
	{
		private const string PreferredLaunchViewWidthKey = "__Uno.PreferredLaunchViewSizeKey.Width";
		private const string PreferredLaunchViewHeightKey = "__Uno.PreferredLaunchViewSizeKey.Height";

		private static readonly Dictionary<WindowId, ApplicationView> _windowIdMap = new();

		private ApplicationViewTitleBar _titleBar = new ApplicationViewTitleBar();
		private IReadOnlyList<Rect> _defaultSpanningRects;
		private IApplicationViewSpanningRects _applicationViewSpanningRects;

		[global::Uno.NotImplemented]
		public int Id => 1;

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

		private Rect _visibleBounds;
		public Foundation.Rect VisibleBounds { get => VisibleBoundsOverride ?? _visibleBounds; private set => _visibleBounds = value; }

		/// <summary>
		/// All other platforms: equivalent to <see cref="VisibleBounds"/>.
		///
		/// Android: returns the visible bounds taking the status bar into account. The status bar is not removed from <see cref="VisibleBounds"/>
		/// on Android when it's opaque, on the grounds that the root managed view is already arranged below the status bar in y-direction by
		/// default (unlike iOS), but in some cases the correct total height is needed, hence this property.
		/// </summary>
		internal Rect TrueVisibleBounds =>
#if __ANDROID__
			VisibleBoundsOverride ?? _trueVisibleBounds;
#else
			VisibleBounds;
#endif

		/// <summary>
		/// If set, overrides the 'real' visible bounds. Used for testing visible bounds-related behavior on devices that have no native
		/// 'unsafe area'.
		/// </summary>
		internal Rect? VisibleBoundsOverride
		{
			get;
			set;
		}

		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.ApplicationView, object> VisibleBoundsChanged;

		[global::Uno.NotImplemented]
		public bool IsFullScreenMode => true;

		public global::Windows.UI.ViewManagement.ApplicationViewTitleBar TitleBar => _titleBar;

		public static global::Windows.UI.ViewManagement.ApplicationView GetForCurrentView() => GetForWindowId(AppWindow.MainWindowId);

#pragma warning disable RS0030 // Do not use banned APIs
		public static global::Windows.UI.ViewManagement.ApplicationView GetForCurrentViewSafe() => GetForCurrentView();
#pragma warning restore RS0030 // Do not use banned APIs

		internal static global::Windows.UI.ViewManagement.ApplicationView IShouldntUseGetForCurrentView() => GetForWindowId(AppWindow.MainWindowId); //TODO:MZ: Does not make sense in WinAppSDK

		internal static global::Windows.UI.ViewManagement.ApplicationView GetForWindowId(WindowId windowId) => _windowIdMap[windowId];

		internal static void InitializeForWindowId(WindowId windowId)
		{
			if (!_windowIdMap.ContainsKey(windowId))
			{
				ApplicationView applicationView = new();
				_windowIdMap[windowId] = applicationView;
			}
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

		internal void SetVisibleBounds(Rect newVisibleBounds)
		{
			if (newVisibleBounds != VisibleBounds)
			{
				VisibleBounds = newVisibleBounds;

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Updated visible bounds {VisibleBounds}");
				}

				VisibleBoundsChanged?.Invoke(this, null);
			}
		}
	}
}
