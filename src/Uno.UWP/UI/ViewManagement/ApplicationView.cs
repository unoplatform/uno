#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;

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

		public Foundation.Rect VisibleBounds { get; internal set; }

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
	}
}
