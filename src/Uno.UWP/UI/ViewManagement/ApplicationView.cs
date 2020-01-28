#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
	public partial class ApplicationView
	{
		private static ApplicationView _instance = new ApplicationView();

		private ApplicationViewTitleBar _titleBar = new ApplicationViewTitleBar();
		private IReadOnlyList<Rect> _spanningRects = new List<Rect>(0);

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

		internal IReadOnlyList<Rect> GetSpanningRects()
		{
			UpdateSpanningRects();
			return _spanningRects;
		}

		partial void UpdateSpanningRects();
	}
}
