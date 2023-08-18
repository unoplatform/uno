#nullable enable

using System;
using DirectUI;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using _Debug = System.Diagnostics.Debug;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ToolTip : ContentControl
	{
		private const double TOOLTIP_TOLERANCE = 2.0;    // Used in PlacementMode.Mouse positioning to avoid screen edges.
		private const double DEFAULT_KEYBOARD_OFFSET = 12;  // Default offset for automatic tooltips opened by keyboard.
		private const double DEFAULT_MOUSE_OFFSET = 20;   // Default offset for automatic tooltips opened by mouse.
		private const double DEFAULT_TOUCH_OFFSET = 44;  // Default offset for automatic tooltips opened by touch.

		//private const double CONTEXT_MENU_HINT_VERTICAL_OFFSET = -5;    // The hint is slightly above center in the vertical axis.

		private const int m_mousePlacementVerticalOffset = 11;  // Mouse placement vertical offset

		internal const PlacementMode DefaultPlacementMode = PlacementMode.Top;

		private Popup? _popup;

		private DependencyObject? _owner;

		internal Popup Popup
		{
			get
			{
				if (_popup == null)
				{
					_popup = new Popup { IsLightDismissEnabled = false, PropagatesDataContextToChild = false };
					_popup.PopupPanel = new PopupPanel(_popup);
					_popup.Opened += OnPopupOpened;
					_popup.Closed += (sender, e) => IsOpen = false;
				}

				return _popup;
			}
		}

		internal long CurrentHoverId { get; set; }

		internal IDisposable? OwnerEventSubscriptions { get; set; }

		internal IDisposable? OwnerVisibilitySubscription { get; set; }

#pragma warning disable CS0649
#pragma warning disable CS0169
#pragma warning disable CS0414
		private PlacementMode? m_pToolTipServicePlacementModeOverride;
		private bool m_bIsPopupPositioned;
		//private bool m_bClosing;
		//private bool m_bIsOpenAsAutomaticToolTip;
		private bool m_bCallPerformPlacementAtNextPopupOpen;
		internal AutomaticToolTipInputMode m_inputMode = AutomaticToolTipInputMode.Mouse; // TODO Uno: This should be set from ToolTipService
		internal bool m_isSliderThumbToolTip;
#pragma warning restore CS0649
#pragma warning restore CS0169
#pragma warning restore CS0414

		private FrameworkElement? Target => _owner as FrameworkElement;

		public ToolTip()
		{
			DefaultStyleKey = typeof(ToolTip);

			SizeChanged += OnToolTipSizeChanged;
			Loading += (sender, e) => PerformPlacementInternal(); // Update placement on Loading, because this is the point at which Uno sets the default Style
		}

		public static DependencyProperty PlacementProperty { get; } =
			DependencyProperty.Register(
				"Placement", typeof(PlacementMode),
				typeof(ToolTip),
				new FrameworkPropertyMetadata(PlacementMode.Top));

		public PlacementMode Placement
		{
			get => (PlacementMode)GetValue(PlacementProperty);
			set => SetValue(PlacementProperty, value);
		}

		public static DependencyProperty IsOpenProperty { get; } =
			DependencyProperty.Register(
				"IsOpen", typeof(bool),
				typeof(ToolTip),
				new FrameworkPropertyMetadata(default(bool), OnOpenChanged));

		public bool IsOpen
		{
			get => (bool)GetValue(IsOpenProperty);
			set => SetValue(IsOpenProperty, value);
		}

		private static void OnOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => (sender as ToolTip)?.OnOpenChanged((bool)args.NewValue);

		private Size GetTooltipSize()
		{
			if (ActualHeight != 0 && ActualWidth != 0)
			{
				return new Size(ActualWidth, ActualHeight);
			}

			if (DesiredSize == default(Size))
			{
				// Ensure we have a correct size before layouting ToolTip, otherwise it may appear under mouse, steal focus and dismiss itself
				ApplyTemplate();
				var availableSize =
					XamlRoot?.Size ??
					(_owner as FrameworkElement)?.XamlRoot?.Size ??
					Xaml.Window.CurrentSafe?.Bounds.Size ??
					default;
				Measure(availableSize);
			}

			return DesiredSize;
		}

		private void OnOpenChanged(bool isOpen)
		{
			PerformPlacementInternal();
			if (_owner is not null)
			{
				XamlRoot = XamlRoot.GetForElement(_owner);
				Popup.XamlRoot = XamlRoot.GetForElement(_owner);
			}
			Popup.IsOpen = isOpen;
			if (isOpen && IsEnabled)
			{
				AttachToPopup();

				Opened?.Invoke(this, new RoutedEventArgs(this));
				GoToElementState("Opened", useTransitions: true);

				if (_owner is FrameworkElement fe)
				{
					// Propagate the DC once, the inheritance
					// will update the rest on DC changes.
					this.SetValue(DataContextProperty, fe.DataContext, DependencyPropertyValuePrecedences.Inheritance);
				}
			}
			else
			{
				Closed?.Invoke(this, new RoutedEventArgs(this));
				GoToElementState("Closed", useTransitions: true);
			}
		}

		private void AttachToPopup()
		{
			if (Parent == null)
			{
				var previousParent = this.GetParent();

				Popup.Child = this;

				// The tooltip is integrated into the PopupPanel, which automatically
				// propagates its own datacontext through inheritance. This ends up changing
				// the DataContext of the tooltip to a local value which cannot be overriden
				// through the tooltip's own actual parent (the control that owns the TooltipService).
				// Restoring the original parent ensures that an incorrect DataContext property will not flow through.
				this.SetParent(previousParent);
			}
			else if (!ReferenceEquals(Parent, _popup))
			{
				this.Log().Warn($"This ToolTip is already in visual tree: won't be able to use it with TooltipService.");
			}
		}

		public void SetAnchor(UIElement element) => _owner = element;

		public event RoutedEventHandler? Closed;

		public event RoutedEventHandler? Opened;

		// Sets the location of the ToolTip's Popup.
		//
		// Slider "Disambiguation UI" ToolTips need special handling since they need to remain centered
		// over the sliding Thumb, which has not yet rendered in its new position.  Therefore, we pass
		// the new target rect to handle this case.
		internal void PerformPlacement(Rect? pTargetRect = null)
		{
			// It is possible for this function to be called even though the ToolTip is closed.
			// This can happen if the ToolTip gets closed before it has had a chance to layout and complete its opening sequence.
			// If this happens, we don't want to continue opening, as that could result in a "closed" ToolTip that is still visible on the screen.
			var spPopup = _popup;

			if (spPopup != null && IsOpen)
			{
				// UNO TODO
				//if (static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->IsWindowed())
				//{
				//	// Sets the location of the ToolTip's Popup out of the Xaml window.
				//	PerformPlacementWithWindowedPopup(pTargetRect);
				//}
				//else
				{
					// Sets the location of the ToolTip's Popup within the Xaml window.
					PerformPlacementWithPopup(pTargetRect);
				}

				// If PerformPlacementWithPopup/PerformPlacementWithWindowedPopup fail to position the Popup, they will set ToolTip.IsOpen=False.
				// If this happens, we don't want to continue opening the ToolTip.
				if (!m_bIsPopupPositioned && IsOpen)
				{
					m_bIsPopupPositioned = true;
					UpdateVisualState();
				}
			}

		}

		//// Sets the location of the ToolTip's Popup within the Xaml window.
		private void PerformPlacementWithPopup(
			Rect? pTargetRect)
		{

			// Make sure we can actually place the ToolTip.  The size should be > 0, and
			// IsOpen and IsEnabled should be true.
			if (IsOpen == false ||
				IsEnabled == false
				// UNO TODO
				//||
				//!DoubleUtil::GreaterThan(tooltipActualWidth, 0) ||
				//!DoubleUtil::GreaterThan(tooltipActualHeight, 0)
				)
			{
				return;
			}

			// If the ToolTipService is opening the ToolTip, then its Placement is used,
			// regardless of what the ToolTip.Placement has been set to.
			var placement = m_pToolTipServicePlacementModeOverride ?? Placement;

			var dimentions = new Rect(HorizontalOffset, VerticalOffset, ActualWidth, ActualHeight);

			// PlacementMode.Mouse only makes sense for automatic ToolTips opened by touch or mouse.
			if (placement == PlacementMode.Mouse &&
				(m_inputMode == AutomaticToolTipInputMode.Touch || m_inputMode == AutomaticToolTipInputMode.Mouse))
			{
				PerformMousePlacementWithPopup(dimentions, placement);
			}
			else
			{
				PerformNonMousePlacementWithPopup(pTargetRect, dimentions, placement);
			}
		}

		// Sets the location of the ToolTip's Popup within the Xaml window.
		private void PerformMousePlacementWithPopup(
			Rect pDimentions, PlacementMode placement)
		{
			double tooltipActualWidth = pDimentions.Right;
			double tooltipActualHeight = pDimentions.Bottom;
			double horizontalOffset = pDimentions.Left;
			double verticalOffset = pDimentions.Top;

			var screenWidth = 0d;
			var screenHeight = 0d;
			var bIsRTL = false;

			double maxX = 0.0;
			double maxY = 0.0;
			double left = 0.0;
			double top = 0.0;
			var toolTipRect = default(Rect);
			var intersectionRect = default(Rect);

			var bounds = XamlRoot?.VisualTree.VisibleBounds ?? Target?.XamlRoot?.VisualTree.VisibleBounds ?? Xaml.Window.CurrentSafe?.Bounds ?? default;
			screenWidth = bounds.Width;
			screenHeight = bounds.Height;

			var spTarget = Target;

			if (spTarget != null)
			{
				// UNO TODO
				//        xaml::FlowDirection targetFlowDirection = xaml::FlowDirection_LeftToRight;
				//spTarget->get_FlowDirection(&targetFlowDirection);
				//bIsRTL = targetFlowDirection == xaml::FlowDirection_RightToLeft;
				bIsRTL = false;

				// We should not do placement if the target is no longer in the live tree.
				if (!spTarget.IsLoaded)
				{
					return;
				}
			}

			var spPopup = Popup;

			var lastPointerEnteredPoint = CoreWindow.GetForCurrentThread()!.PointerPosition;

			left = lastPointerEnteredPoint.X;
			top = lastPointerEnteredPoint.Y;

			// If we are in RTL mode, then flip the X coordinate around so that it appears to be in LTR mode. That
			// means all of the LTR logic will still work.
			if (bIsRTL)
			{
				left = screenWidth - left;
			}

			MovePointToPointerToolTipShowPosition(ref left, ref top, placement);

			// align ToolTip with the bottom left corner of mouse bounding rectangle
			top += m_mousePlacementVerticalOffset + verticalOffset;
			left += horizontalOffset;

			// pessimistic check of top value - can be 0 only if TextBlock().FontSize == 0
			top = Math.Max(TOOLTIP_TOLERANCE, top);

			// left can be less then TOOLTIP_tolerance if user put mouse pointer on the border of object
			left = Math.Max(TOOLTIP_TOLERANCE, left);

			maxX = screenWidth;
			maxY = screenHeight;

			toolTipRect.X = left;
			toolTipRect.Y = top;
			toolTipRect.Width = tooltipActualWidth;
			toolTipRect.Height = tooltipActualHeight;

			intersectionRect.Width = maxX;
			intersectionRect.Height = maxY;

			intersectionRect.Intersect(toolTipRect);
			if ((Math.Abs(intersectionRect.Width - toolTipRect.Width) < TOOLTIP_TOLERANCE) &&
				(Math.Abs(intersectionRect.Height - toolTipRect.Height) < TOOLTIP_TOLERANCE))
			{
				// The placement algorithm operates in LTR mode (with transformed data if it
				// is really in RTL mode), so we also need to transform the X value it returns.
				if (bIsRTL)
				{
					left = screenWidth - left;
				}

				// ToolTip is completely inside the plug-in
				spPopup.VerticalOffset = top;
				spPopup.HorizontalOffset = left;
			}
			else
			{
				if (top + toolTipRect.Height > maxY)
				{
					// If the lower edge of the plug-in obscures the ToolTip,
					// it repositions itself to align with the upper edge of the bounding box of the mouse.
					top = maxY - toolTipRect.Height - TOOLTIP_TOLERANCE;
				}

				if (top < 0)
				{
					// If the upper edge of Plug-in obscures the ToolTip,
					// the control repositions itself to align with the upper edge.
					// align with the top of the plug-in
					top = 0;
				}

				if (left + toolTipRect.Width > maxX)
				{
					// If the right edge obscures the ToolTip,
					// it opens in the opposite direction from the obscuring edge.
					left = maxX - toolTipRect.Width - TOOLTIP_TOLERANCE;
				}

				if (left < 0)
				{
					// If the left edge obscures the ToolTip,
					// it then aligns with the obscuring screen edge
					left = 0;
				}

				// if right/bottom doesn't fit into the plug-in bounds, clip the ToolTip
				{
					var clipCalculationsRect = new Rect(left, top, toolTipRect.Width, toolTipRect.Height);
					CalculateTooltipClip(clipCalculationsRect, maxX, maxY);
				}

				// The placement algorithm operates in LTR mode (with transformed data if it
				// is really in RTL mode), so we also need to transform the X value it returns.

				if (bIsRTL)
				{
					left = screenWidth - left;
				}

				// position the parent Popup
				spPopup.VerticalOffset = top + verticalOffset;
				spPopup.HorizontalOffset = left + horizontalOffset;
			}
		}

		// Sets the location of the ToolTip's Popup within the Xaml window.
		private void PerformNonMousePlacementWithPopup(
			Rect? pTargetRect, Rect pDimentions, PlacementMode placement)
		{
			var horizontalOffset = pDimentions.Left;
			var verticalOffset = pDimentions.Top;

			var bIsRTL = false;

			var origin = default(Point);
			var rcDockTo = default(Rect);
			var szFlyout = default(Size);

			var spTarget = _owner as FrameworkElement;
			_Debug.Assert(spTarget != null, "pTarget expected to be non-null in ToolTip_Partial::PerformPlacement()");
			if (spTarget != null)
			{
				// UNO TODO
				//	        xaml::FlowDirection targetFlowDirection = xaml::FlowDirection_LeftToRight;

				//spTarget->get_FlowDirection(&targetFlowDirection);
				//bIsRTL = targetFlowDirection == xaml::FlowDirection_RightToLeft;

				// We should not do placement if the target is no longer in the live tree.
				if (!spTarget.IsLoaded)
				{
					return;
				}
			}

			var spPopup = _popup;
			if (spPopup == null)
			{
				return;
			}

			// For ToolTips opened by keyboard focus, PlacementMode_Mouse doesn't make any sense.
			// Fall back to the default - PlacementMode_Top.
			if (placement == PlacementMode.Mouse)
			{
				placement = PlacementMode.Top;
			}

			if (spTarget == null)
			{
				return;
			}

			var visibleRect = XamlRoot?.VisualTree.VisibleBounds ?? spTarget?.XamlRoot?.VisualTree.VisibleBounds ?? Xaml.Window.CurrentSafe?.Bounds ?? default;
			var constraint = visibleRect;

			var windowRect = XamlRoot?.VisualTree.VisibleBounds ?? spTarget?.XamlRoot?.VisualTree.VisibleBounds ?? Xaml.Window.CurrentSafe?.Bounds ?? default;
			origin.X = windowRect.X;
			origin.Y = windowRect.Y;

			szFlyout = GetTooltipSize();

			var placementRect = GetPlacementRectInWindowCoordinates();

			var getDockToRectFromTargetElement = false;

			if (pTargetRect.HasValue)
			{
				// Slider case - position ToolTip over Thumb rect
				rcDockTo = pTargetRect.Value;
			}
			else if (!placementRect.IsEmpty)
			{
				rcDockTo = placementRect;
			}
			else if (!m_isSliderThumbToolTip &&
				(AutomaticToolTipInputMode.Touch == m_inputMode || AutomaticToolTipInputMode.Mouse == m_inputMode))
			{
				var lastPointerEnteredPoint = default(Point);

				// UNO TODO: PointerPoint.GetCurrentPoint(uint pointerId)
				lastPointerEnteredPoint = CoreWindow.GetForCurrentThread()!.PointerPosition;

				rcDockTo.X = lastPointerEnteredPoint.X;
				rcDockTo.Y = lastPointerEnteredPoint.Y;

				// UNO TODO
				//// For touch, we need to account for the fact that the context menu hint has a vertical offset.
				//if (AutomaticToolTipInputMode::Touch == m_inputMode)
				//{
				//    rcDockTo.Top += CONTEXT_MENU_HINT_VERTICAL_OFFSET;
				//    rcDockTo.bottom += CONTEXT_MENU_HINT_VERTICAL_OFFSET;
				//}
			}
			else
			{
				getDockToRectFromTargetElement = true;
			}

			if (getDockToRectFromTargetElement && Target != null)
			{
				var targetTopLeft = default(Point);
				targetTopLeft = Target.TransformToVisual(null).TransformPoint(targetTopLeft);

				//// UNO TODO
				//      // targetTopLeft.X should be the left edge of the target, so adjust for RTL by
				//      // subtracting the width of the target.
				//      if (bIsRTL)
				//      {
				//          targetTopLeft.X -= targetActualWidth;
				//      }

				rcDockTo.X = targetTopLeft.X;
				rcDockTo.Y = targetTopLeft.Y;
				rcDockTo.Width = Target.ActualWidth;
				rcDockTo.Height = Target.ActualHeight;
			}

			rcDockTo = rcDockTo.OffsetRect(origin.X, origin.Y);

			// If horizontal & vertical offset are not specified, use the system defaults.
			var isPropertyLocal = this.IsDependencyPropertySet(HorizontalOffsetProperty);
			if (!isPropertyLocal)
			{
				horizontalOffset = DEFAULT_MOUSE_OFFSET;
				switch (m_inputMode)
				{
					case AutomaticToolTipInputMode.Keyboard:
						horizontalOffset = DEFAULT_KEYBOARD_OFFSET;
						break;
					case AutomaticToolTipInputMode.Mouse:
						horizontalOffset = DEFAULT_MOUSE_OFFSET;
						break;
					case AutomaticToolTipInputMode.Touch:
						horizontalOffset = DEFAULT_TOUCH_OFFSET;
						break;
				}
			}

			isPropertyLocal = this.IsDependencyPropertySet(VerticalOffsetProperty);
			if (!isPropertyLocal)
			{
				verticalOffset = DEFAULT_MOUSE_OFFSET;
				switch (m_inputMode)
				{
					case AutomaticToolTipInputMode.Keyboard:
						verticalOffset = DEFAULT_KEYBOARD_OFFSET;
						break;
					case AutomaticToolTipInputMode.Mouse:
						verticalOffset = DEFAULT_MOUSE_OFFSET;
						break;
					case AutomaticToolTipInputMode.Touch:
						verticalOffset = DEFAULT_TOUCH_OFFSET;
						break;
				}
			}

			// UNO TODO
			//// Reverse Left/Right palacement for RTL, because the target is flipped.
			//if (bIsRTL)
			//{
			//    if (placement == PlacementMode.Right)
			//    {
			//        placement = PlacementMode.Left;
			//    }
			//    else if (placement == PlacementMode.Left)
			//    {
			//        placement = PlacementMode.Right;
			//    }
			//}

			// To honor horizontal/vertical offset, inflate the placement target by these offsets before calling into
			// the Windows popup positioning logic.
			rcDockTo.Inflate(horizontalOffset, verticalOffset);
			(var rcResult, var placementChosen) = ToolTipPositioning.QueryRelativePosition(
				constraint,
				szFlyout,
				rcDockTo,
				placement);

			// if right/bottom doesn't fit into the plug-in bounds, clip the ToolTip
			{
				var clipCalculationsRect = new Rect(0, 0, ActualWidth, ActualHeight);
				CalculateTooltipClip(clipCalculationsRect, visibleRect.Width - visibleRect.X, visibleRect.Height - visibleRect.Y);
			}

			// Position tooltip by setting popup's offsets
			spPopup.VerticalOffset = rcResult.Top - origin.Y;
			// ToolTipPositioning::QueryRelativePosition is used to position in LTR and RTL. In LTR, the horizontal
			// offset is  in rcResult.Left. In RTL, the horizontal offset is in rcResult.Right because the
			// popup is flipped.
			spPopup.HorizontalOffset = (bIsRTL) ? rcResult.Right - origin.X : rcResult.Left - origin.X;

			// There used to be a setting of FromVerticalOffset and FromHorizontalOffset on the ToolTipTemplateSettings
			// gotten from get_TemplateSettings here.  However, this is no longer done because the ToolTip animation
			// is now a FadeIn/FadeOut which doesn't use any FromHorizontalOffset/FromVerticalOffset.  This leaves
			// ToolTipTemplateSettings and all associated code in a basically non-used and deprecated state - but as of
			// now, we can't make any breaking changes.  So, to preserve max compatibility, the ToolTipTemplateSettings
			// is still around as a class/interface/property of the tooltip for now.  It should be removed (or at least
			// the ToolTip-specific-ness of the Tooltip's template settings should be removed) as soon as it's ok to
			// make a breaking change.
		}


		private void CalculateTooltipClip(
			Rect toolTipRect, double maxX, double maxY)
		{
			var clipSize = default(Size);
			var dX = 0.0;
			var dY = 0.0;

			dX = toolTipRect.Left + toolTipRect.Right - maxX;
			dY = toolTipRect.Top + toolTipRect.Bottom - maxY;

			if ((dX >= 0) || (dY >= 0))
			{
				dX = Math.Max(0, dX);
				dY = Math.Max(0, dY);
				clipSize.Width = Math.Max(0, toolTipRect.Right - dX);
				clipSize.Height = Math.Max(0, toolTipRect.Bottom - dY);
				PerformClipping(clipSize);
			}
		}

		private void MovePointToPointerToolTipShowPosition(
			ref Point point,
			PlacementMode placement)
		{
			// If the point is inside the placement rect, move it out of the placement rect.
			var placementRect = GetPlacementRectInWindowCoordinates();

			if (!placementRect.IsEmpty && placementRect.Contains(point))
			{
				switch (placement)
				{
					case PlacementMode.Left:
						point.X = placementRect.X;
						break;
					case PlacementMode.Right:
						point.X = placementRect.X + placementRect.Width;
						break;
					case PlacementMode.Top:
					case PlacementMode.Mouse:
						point.Y = placementRect.Y;
						break;
					case PlacementMode.Bottom:
						point.Y = placementRect.Y + placementRect.Height;
						break;
				}
			}
		}

		private void MovePointToPointerToolTipShowPosition(
			ref double left,
			ref double top,
			PlacementMode placement)
		{
			var point = new Point(left, top);

			MovePointToPointerToolTipShowPosition(ref point, placement);

			left = point.X;
			top = point.Y;
		}

		private Rect GetPlacementRectInWindowCoordinates()
		{
			var placementRectLocal = Rect.Empty;

			if (PlacementRect is Rect placementRect)
			{
				placementRectLocal = placementRect;

				if (Target != null)
				{
					var tr = Target.TransformToVisual(null);
					tr.TransformBounds(placementRectLocal);
				}
			}

			return placementRectLocal;
		}

		// If the owner is a TextElement, we'll get its bounding rect and use that as our placement target rect.
		// Otherwise, we'll use the default rect derived from the target.
		private void PerformPlacementInternal()
		{
			if (_owner is TextElement textElement)
			{
				// UNO TODO
				//        CCoreServices* pCore = static_cast<CCoreServices*>(DXamlCore::GetCurrent()->GetHandle());

				//XRECTF boundingRect;
				//pCore->GetTextElementBoundingRect(ownerAsTextElement.Cast<TextElement>()->GetHandle(), &boundingRect);

				//        RECT rectDockTo =
				//		{
				//			static_cast<long>(boundingRect.X),
				//			static_cast<long>(boundingRect.Y),
				//			static_cast<long>(boundingRect.X + boundingRect.Width),
				//			static_cast<long>(boundingRect.Y + boundingRect.Height)
				//		};
				//PerformPlacement(&rectDockTo);
			}
			else
			{
				PerformPlacement();
			}
		}

		// Handle the SizeChanged event.
		private void OnToolTipSizeChanged(
			object pSender,
			SizeChangedEventArgs pArgs)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{nameof(OnToolTipSizeChanged)} - new Size=({ActualWidth}, {ActualHeight})");
			}
			PerformPlacementInternal();
		}

		private void OnPopupOpened(
				object? pUnused1,
				object pUnused2)
		{
			//OnOpened();

			// UNO TODO
			//// If we've recently closed and reopened this ToolTip, then don't do anything until we receive the final Popup.Opened
			//// event. See comment for m_pendingPopupOpenEventCount in header for details.
			//m_pendingPopupOpenEventCount--;
			//if (m_pendingPopupOpenEventCount == 0 && m_bCallPerformPlacementAtNextPopupOpen)
			{
				m_bCallPerformPlacementAtNextPopupOpen = false;
				PerformPlacementInternal();
			}
		}

		private void PerformClipping(
			Size size)
		{
			// By default a tooltip has only 1 child (border).
			var childCount = VisualTreeHelper.GetChildrenCount(this);
			if (childCount > 0)
			{
				var spChildAsFE = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
				if (spChildAsFE != null)
				{
					if (size.Width < spChildAsFE.ActualWidth)
					{
						spChildAsFE.Width = size.Width;
					}

					if (size.Height < spChildAsFE.ActualHeight)
					{
						spChildAsFE.Height = size.Height;
					}
				}
			}
		}

		// Removes the "automatic" flag and clears associated fields.
		//
		// For Slider, the Thumb ToolTip may be opened as an automatic ToolTip by pointer hover.  However, if
		// we click on the Thumb and start to drag, we don't want the ToolTip to disappear after several seconds.
		// Thus, we remove the automatic flag and keep the ToolTip open for Slider to handle.
		[NotImplemented]
		internal void RemoveAutomaticStatusFromOpenToolTip()
		{
			// TODO Uno: Not implemented yet
		}
	}
}
