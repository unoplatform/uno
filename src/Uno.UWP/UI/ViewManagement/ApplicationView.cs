#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;
using Uno.Extensions;
using Uno.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Windows.UI.ViewManagement
{
	public partial class ApplicationView
		: IApplicationViewSpanningRects
	{
		private static ApplicationView _instance = new ApplicationView();

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

		public Foundation.Rect VisibleBounds { get; private set; }

		/// <summary>
		/// All other platforms: equivalent to <see cref="VisibleBounds"/>.
		///
		/// Android: returns the visible bounds taking the status bar into account. The status bar is not removed from <see cref="VisibleBounds"/>
		/// on Android when it's opaque, on the grounds that the root managed view is already arranged below the status bar in y-direction by
		/// default (unlike iOS), but in some cases the correct total height is needed, hence this property.
		/// </summary>
		internal Rect TrueVisibleBounds =>
#if __ANDROID__
			_trueVisibleBounds;
#else
			VisibleBounds;
#endif

		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.ApplicationView, object> VisibleBoundsChanged;

		[global::Uno.NotImplemented]
		public bool IsFullScreenMode => true;

		public global::Windows.UI.ViewManagement.ApplicationViewTitleBar TitleBar => _titleBar;

		public static global::Windows.UI.ViewManagement.ApplicationView GetForCurrentView() => _instance;

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

		public bool IsViewModeSupported(ApplicationViewMode viewMode)
		{
			if (viewMode == ApplicationViewMode.Default)
			{
				return true;
			}
			else if (viewMode == ApplicationViewMode.Spanning)
			{
				return (_applicationViewSpanningRects as INativeDualScreenProvider)?.IsDualScreen == true;
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

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
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

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Updated visible bounds {VisibleBounds}");
				}

				VisibleBoundsChanged?.Invoke(this, null);
			}
		}
	}
}
