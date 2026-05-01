#if IS_UNIT_TESTS
#pragma warning disable CS0067
#endif

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.Foundation.Metadata;
using Uno.UI.Xaml.Core;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __APPLE_UIKIT__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.ScrollContentPresenter;
#else
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.IScrollContentPresenter;
#endif

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer : ContentControl, IFrameworkTemplatePoolAware
	{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
		private bool m_isInConstantVelocityPan;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

		private static class Parts
		{
			public static class Uwp
			{
				public const string ScrollContentPresenter = "ScrollContentPresenter";
				public const string VerticalScrollBar = "VerticalScrollBar";
				public const string HorizontalScrollBar = "HorizontalScrollBar";
			}

			public static class WinUI3
			{
				public const string Scroller = "PART_Scroller";
				public const string VerticalScrollBar = "PART_VerticalScrollBar";
				public const string HorizontalScrollBar = "PART_HorizontalScrollBar";
			}
		}

		private static class VisualStates
		{
			public static class ScrollingIndicator
			{
				public const string None = "NoIndicator";
				public const string Touch = "TouchIndicator";
				public const string Mouse = "MouseIndicator";
				// public const string MouseFull = "MouseIndicatorFull"; // No supported yet
			}

			public static class ScrollBarsSeparator
			{
				public const string Collapsed = "ScrollBarSeparatorCollapsed";
				public const string Expanded = "ScrollBarSeparatorExpanded";
				public const string ExpandedWithoutAnimation = "ScrollBarSeparatorExpandedWithoutAnimation";
				public const string CollapsedWithoutAnimation = "ScrollBarSeparatorCollapsedWithoutAnimation";

				// On WinUI3 visuals states are prefixed with "ScrollBar***s***" (with a trailing 's')
				//public const string Collapsed = "ScrollBarsSeparatorCollapsed";
				//public const string CollapsedDisabled = "ScrollBarsSeparatorCollapsedDisabled"; // Not supported yet
				//public const string Expanded = "ScrollBarsSeparatorExpanded";
				//public const string DisplayedWithoutAnimation = "ScrollBarsSeparatorDisplayedWithoutAnimation"; // Not supported yet
				//public const string ExpandedWithoutAnimation = "ScrollBarsSeparatorExpandedWithoutAnimation";
				//public const string CollapsedWithoutAnimation = "ScrollBarsSeparatorCollapsedWithoutAnimation";
			}
		}

		private static bool IsAnimationEnabled => Uno.UI.Helpers.WinUI.SharedHelpers.IsAnimationsEnabled();

		/// <summary>
		/// Occurs when manipulations such as scrolling and zooming have caused the view to change.
		/// </summary>
		public event EventHandler<ScrollViewerViewChangedEventArgs>? ViewChanged;

		internal event SizeChangedEventHandler? ExtentSizeChanged;

		public ScrollViewer()
		{
			DefaultStyleKey = typeof(ScrollViewer);

			UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewer.GetUpdatesMode(this);
			InitializePartial();
		}

		partial void InitializePartial();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			EnsureAttachScrollBars();

			OnLoadedPartial();
		}

		private partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			DetachScrollBars();
			ResetScrollIndicator();

			OnUnloadedPartial();
		}
		private partial void OnUnloadedPartial();

		protected override AutomationPeer OnCreateAutomationPeer() => new ScrollViewerAutomationPeer(this);

		// Note: All DependencyProperty declarations have been moved to ScrollViewer.Properties.cs
		// as a sideline foundation step. ScrollViewer.cs retains only behavioral logic and field state.


		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

#pragma warning disable 649 // unused member for Unit tests
		private _ScrollContentPresenter? _presenter;
#pragma warning restore 649 // unused member for Unit tests

		/// <summary>
		/// Gets the ScrollContentPresenter resolved from the template.
		/// Be aware that on iOS and Android this might be only a wrapper onto the NativeScrollContentPresenter.
		/// </summary>
		/// <remarks>
		/// This is a temporary workaround until the NativeSCP knows its managed SCP and will most probably been removed in a near .
		/// Try to avoid usage of this property as much as possible!
		/// </remarks>
		internal ScrollContentPresenter? Presenter { get; private set; }

		/// <summary>
		/// Gets the size of the Viewport used in the **CURRENT** (cf. remarks) or last measure
		/// </summary>
		/// <remarks>Unlike the LayoutInformation.GetAvailableSize(), this property is set **BEFORE** measuring the children of the ScrollViewer</remarks>
		internal Size ViewportMeasureSize { get; private set; }

		/// <summary>
		/// Gets the size of the Viewport used in the **CURRENT** (cf. remarks) or last arrange
		/// </summary>
		/// <remarks>Unlike the LayoutInformation.GetLayoutSlot(), this property is set **BEFORE** arranging the children of the ScrollViewer</remarks>
		internal Size ViewportArrangeSize { get; private set; }

		/// <summary>
		/// Cached value of <see cref="Uno.UI.Xaml.Controls.ScrollViewer.UpdatesModeProperty"/>,
		/// in order to not access the DP on each scroll (perf considerations)
		/// </summary>
		internal Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode UpdatesMode { get; set; }

		/// <summary>
		/// If this flag is enabled, the ScrollViewer will report offsets less than 0 and greater than <see cref="ScrollableHeight"/> when
		/// 'overscrolling' on iOS. By default this is false, matching Windows behaviour.
		/// </summary>
		[UnoOnly]
		public bool ShouldReportNegativeOffsets { get; set; } = false;

		/// <summary>
		/// Determines if the vertical scrolling is allowed or not.
		/// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		/// </summary>
		internal bool ComputedIsHorizontalScrollEnabled { get; private set; }

		/// <summary>
		/// Determines if the vertical scrolling is allowed or not.
		/// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		/// </summary>
		internal bool ComputedIsVerticalScrollEnabled { get; private set; }

		internal double MinHorizontalOffset => 0;

		internal double MinVerticalOffset => 0;



		/// <summary>
		/// Sets the content of the ScrollViewer
		/// </summary>
		/// <param name="view"></param>
		/// <remarks>Used in the context of member initialization</remarks>
		public
#if !UNO_REFERENCE_API && !IS_UNIT_TESTS
			new
#endif
			void Add(View view)
		{
			Content = view;
		}



		/// <summary>
		/// Determines whether this ScrollViewer is pannable.
		/// Returns false only if both vertical and horizontal scrolling are disabled.
		/// </summary>
		internal override bool IsDraggableOrPannable()
		{
			// If both vertical and horizontal scrolling are disabled, return false.
			// This matches WinUI's ScrollViewer::IsDraggableOrPannableImpl.
			return !(
				(VerticalScrollBarVisibility == ScrollBarVisibility.Disabled ||
					VerticalScrollMode == ScrollMode.Disabled)
				&&
				(HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled ||
					HorizontalScrollMode == ScrollMode.Disabled)
			);
		}
	}
}
