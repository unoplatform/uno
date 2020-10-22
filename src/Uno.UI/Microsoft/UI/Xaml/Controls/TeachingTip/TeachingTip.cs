using System;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TeachingTip : ContentControl
	{
		


		public TeachingTip()
		{
			m_target = this;

			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_TeachingTip);
			DefaultStyleKey = typeof(TeachingTip);
			EnsureProperties();
			Unloaded += ClosePopupOnUnloadEvent;
			m_automationNameChangedRevoker = RegisterPropertyChangedCallback(AutomationProperties.NameProperty, OnAutomationNameChanged);
			m_automationIdChangedRevoker = RegisterPropertyChangedCallback(AutomationProperties.AutomationIdProperty, OnAutomationIdChanged);
			SetValue(TemplateSettingsProperty, new TeachingTipTemplateSettings());
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TeachingTipAutomationPeer(this);
		}

				void OnApplyTemplate()
				{
					m_acceleratorKeyActivatedRevoker.revoke();
					m_effectiveViewportChangedRevoker.revoke();
					m_contentSizeChangedRevoker.revoke();
					m_closeButtonClickedRevoker.revoke();
					m_alternateCloseButtonClickedRevoker.revoke();
					m_actionButtonClickedRevoker.revoke();
					m_windowSizeChangedRevoker.revoke();

					//IControlProtected controlProtected{ this };

					m_container = (Border)GetTemplateChild(s_containerName);
					m_rootElement = m_container.Child();
					m_tailOcclusionGrid = (Grid)GetTemplateChild(s_tailOcclusionGridName);
					m_contentRootGrid = (Grid)GetTemplateChild(s_contentRootGridName);
					m_nonHeroContentRootGrid = (Grid)GetTemplateChild(s_nonHeroContentRootGridName);
					m_heroContentBorder = (Border)GetTemplateChild(s_heroContentBorderName);
					m_actionButton = (Button)GetTemplateChild(s_actionButtonName);
					m_alternateCloseButton = (Button)GetTemplateChild(s_alternateCloseButtonName);
					m_closeButton = (Button)GetTemplateChild(s_closeButtonName);
					m_tailEdgeBorder = (Grid)GetTemplateChild(s_tailEdgeBorderName);
					m_tailPolygon = (Polygon)GetTemplateChild(s_tailPolygonName);
					m_titleTextBox = (UIElement)GetTemplateChild(s_titleTextBoxName);
					m_subtitleTextBox = (UIElement)GetTemplateChild(s_subtitleTextBoxName);

					if (var container = m_container)
		    {
						container.Child(null);
					}

					m_contentSizeChangedRevoker = [this]()

			{
						if (var tailOcclusionGrid = m_tailOcclusionGrid)
		        {
							return tailOcclusionGrid.SizeChanged(auto_revoke, { this, &TeachingTip.OnContentSizeChanged });
						}
						return FrameworkElement.SizeChanged_revoker{ };
					} ();

					if (var contentRootGrid = m_contentRootGrid)
		    {
						AutomationProperties.SetLocalizedLandmarkType(contentRootGrid, ResourceAccessor.GetLocalizedStringResource(SR_TeachingTipCustomLandmarkName));
					}

					m_closeButtonClickedRevoker = [this]()

			{
						if (var closeButton = m_closeButton)
		        {
							return closeButton.Click(auto_revoke, { this, &TeachingTip.OnCloseButtonClicked });
						}
						return Button.Click_revoker{ };
					} ();

					m_alternateCloseButtonClickedRevoker = [this]()

			{
						if (var alternateCloseButton = m_alternateCloseButton)
		        {
							AutomationProperties.SetName(alternateCloseButton, ResourceAccessor.GetLocalizedStringResource(SR_TeachingTipAlternateCloseButtonName));
							ToolTip tooltip = ToolTip();
							tooltip.ContentResourceAccessor.GetLocalizedStringResource(SR_TeachingTipAlternateCloseButtonTooltip));
							ToolTipService.SetToolTip(alternateCloseButton, tooltip);
							return alternateCloseButton.Click(auto_revoke, { this, &TeachingTip.OnCloseButtonClicked });
						}
						return Button.Click_revoker{ };
					} ();

					m_actionButtonClickedRevoker = [this]()

			{
						if (var actionButton = m_actionButton)
		        {
							return actionButton.Click(auto_revoke, { this, &TeachingTip.OnActionButtonClicked });
						}
						return Button.Click_revoker{ };
					} ();

					UpdateButtonsState();
					OnIsLightDismissEnabledChanged();
					OnIconSourceChanged();

					EstablishShadows();

					m_isTemplateApplied = true;
				}

				void OnPropertyChanged(DependencyPropertyChangedEventArgs& args)
				{
					IDependencyProperty property = args.Property();

					if (property == s_IsOpenProperty)
					{
						OnIsOpenChanged();
					}
					else if (property == s_TargetProperty)
					{
						// Unregister from old target if it exists
						if (args.OldValue())
						{
							m_TargetUnloadedRevoker.revoke();
						}

						// Register to new target if it exists
						if (var value = args.NewValue()) {
							FrameworkElement newTarget = (FrameworkElement)value;
							m_TargetUnloadedRevoker = newTarget.Unloaded(auto_revoke, { this,&TeachingTip.ClosePopupOnUnloadEvent });
						}
						OnTargetChanged();
					}
					else if (property == s_ActionButtonContentProperty ||
						property == s_CloseButtonContentProperty)
					{
						UpdateButtonsState();
					}
					else if (property == s_PlacementMarginProperty)
					{
						OnPlacementMarginChanged();
					}
					else if (property == s_IsLightDismissEnabledProperty)
					{
						OnIsLightDismissEnabledChanged();
					}
					else if (property == s_ShouldConstrainToRootBoundsProperty)
					{
						OnShouldConstrainToRootBoundsChanged();
					}
					else if (property == s_TailVisibilityProperty)
					{
						OnTailVisibilityChanged();
					}
					else if (property == s_PreferredPlacementProperty)
					{
						if (IsOpen())
						{
							PositionPopup();
						}
					}
					else if (property == s_HeroContentPlacementProperty)
					{
						OnHeroContentPlacementChanged();
					}
					else if (property == s_IconSourceProperty)
					{
						OnIconSourceChanged();
					}
					else if (property == s_TitleProperty)
					{
						SetPopupAutomationProperties();
						if (ToggleVisibilityForEmptyContent(m_titleTextBox, Title()))
						{
							TeachingTipTestHooks.NotifyTitleVisibilityChanged(this);
						}
					}
					else if (property == s_SubtitleProperty)
					{
						if (ToggleVisibilityForEmptyContent(m_subtitleTextBox, Subtitle()))
						{
							TeachingTipTestHooks.NotifySubtitleVisibilityChanged(this);
						}
					}

				}

		private bool ToggleVisibilityForEmptyContent(UIElement element, string content)
		{
			if (element != null)
			{
				if (content != "")
				{
					if (element.Visibility == Visibility.Collapsed)
					{
						element.Visibility = Visibility.Visible);
						return true;
					}
				}
				else
				{
					if (element.Visibility == Visibility.Visible)
					{
						element.Visibility = Visibility.Collapsed;
						return true;
					}
				}
			}
			return false;
		}

		private void OnContentChanged(object oldContent, object newContent)
		{
			if (newContent != null)
			{
				VisualStateManager.GoToState(this, "Content", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "NoContent", false);
			}
		}

		private void SetPopupAutomationProperties()
		{
			var popup = m_popup;
			if (popup != null)
		    {
				var name = AutomationProperties.GetName(this);
				if (string.IsNullOrEmpty(name))
				{
					name = Title;
				}
				AutomationProperties.SetName(popup, name);

				AutomationProperties.SetAutomationId(popup, AutomationProperties.GetAutomationId(this));
			}
		}

		// Playing a closing animation when the Teaching Tip is closed via light dismiss requires this work around.
		// This is because there is no event that occurs when a popup is closing due to light dismiss so we have no way to intercept
		// the close and play our animation first. To work around this we've created a second popup which has no content and sits
		// underneath the teaching tip and is put into light dismiss mode instead of the primary popup. Then when this popup closes
		// due to light dismiss we know we are supposed to close the primary popup as well. To ensure that this popup does not block
		// interaction to the primary popup we need to make sure that the LightDismissIndicatorPopup is always opened first, so that
		// it is Z ordered underneath the primary popup.
		private void CreateLightDismissIndicatorPopup()
		{
			var popup = Popup;
			// Set XamlRoot on the popup to handle XamlIsland/AppWindow scenarios.
			UIElement uiElement10 = this;
			if (uiElement10 != null)
		    {
				popup.XamlRoot = uiElement10.XamlRoot;
			}
			// A Popup needs contents to open, so set a child that doesn't do anything.
			var grid = Grid;
			popup.Child = grid;

			m_lightDismissIndicatorPopup = popup;
		}

		//		bool UpdateTail()
		//		{
		//			// An effective placement of var indicates that no tail should be shown.
		//			var[placement, tipDoesNotFit] = DetermineEffectivePlacement();
		//			m_currentEffectiveTailPlacementMode = placement;
		//			var tailVisibility = TailVisibility();
		//			if (tailVisibility == TeachingTipTailVisibility.Collapsed || (!m_target && tailVisibility != TeachingTipTailVisibility.Visible))
		//			{
		//				m_currentEffectiveTailPlacementMode = TeachingTipPlacementMode.Auto;
		//			}

		//			if (placement != m_currentEffectiveTipPlacementMode)
		//			{
		//				m_currentEffectiveTipPlacementMode = placement;
		//				TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
		//			}

		//			var nullableTailOcclusionGrid = m_tailOcclusionGrid;

		//			var height = nullableTailOcclusionGrid ? (float)(nullableTailOcclusionGrid.ActualHeight()) : 0;
		//			var width = nullableTailOcclusionGrid ? (float)(nullableTailOcclusionGrid.ActualWidth()) : 0;

		//			var[firstColumnWidth, secondColumnWidth, nextToLastColumnWidth, lastColumnWidth] = [this, nullableTailOcclusionGrid]()

		//	{
		//				if (var columnDefinitions = nullableTailOcclusionGrid ? nullableTailOcclusionGrid.ColumnDefinitions() : null)
		//        {
		//					var firstColumnWidth = columnDefinitions.Size() > 0 ? (float)(columnDefinitions.GetAt(0).ActualWidth()) : 0.0f;
		//					var secondColumnWidth = columnDefinitions.Size() > 1 ? (float)(columnDefinitions.GetAt(1).ActualWidth()) : 0.0f;
		//					var nextToLastColumnWidth = columnDefinitions.Size() > 1 ? (float)(columnDefinitions.GetAt(columnDefinitions.Size() - 2).ActualWidth()) : 0.0f;
		//					var lastColumnWidth = columnDefinitions.Size() > 0 ? (float)(columnDefinitions.GetAt(columnDefinitions.Size() - 1).ActualWidth()) : 0.0f;

		//					return Tuple.Create(firstColumnWidth, secondColumnWidth, nextToLastColumnWidth, lastColumnWidth);
		//				}
		//				return Tuple.Create(0.0f, 0.0f, 0.0f, 0.0f);
		//			} ();

		//			var[firstRowHeight, secondRowHeight, nextToLastRowHeight, lastRowHeight] = [this, nullableTailOcclusionGrid]()

		//	{
		//				if (var rowDefinitions = nullableTailOcclusionGrid ? nullableTailOcclusionGrid.RowDefinitions() : null)
		//        {
		//					var firstRowHeight = rowDefinitions.Size() > 0 ? (float)(rowDefinitions.GetAt(0).ActualHeight()) : 0.0f;
		//					var secondRowHeight = rowDefinitions.Size() > 1 ? (float)(rowDefinitions.GetAt(1).ActualHeight()) : 0.0f;
		//					var nextToLastRowHeight = rowDefinitions.Size() > 1 ? (float)(rowDefinitions.GetAt(rowDefinitions.Size() - 2).ActualHeight()) : 0.0f;
		//					var lastRowHeight = rowDefinitions.Size() > 0 ? (float)(rowDefinitions.GetAt(rowDefinitions.Size() - 1).ActualHeight()) : 0.0f;

		//					return Tuple.Create(firstRowHeight, secondRowHeight, nextToLastRowHeight, lastRowHeight);
		//				}
		//				return Tuple.Create(0.0f, 0.0f, 0.0f, 0.0f);
		//			} ();

		//			UpdateSizeBasedTemplateSettings();

		//			switch (m_currentEffectiveTailPlacementMode)
		//			{
		//				// An effective placement of var means the tip should not display a tail.
		//				case TeachingTipPlacementMode.Auto:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width / 2, height / 2, 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "Untargeted", false);
		//					break;

		//				case TeachingTipPlacementMode.Top:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width / 2, height - lastRowHeight, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { (width / 2) - firstColumnWidth, 0.0f, 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "Top", false);
		//					break;

		//				case TeachingTipPlacementMode.Bottom:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width / 2, firstRowHeight, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { (width / 2) - firstColumnWidth, 0.0f, 0.0f });
		//					UpdateDynamicHeroContentPlacementToBottom();
		//					VisualStateManager.GoToState(this, "Bottom", false);
		//					break;

		//				case TeachingTipPlacementMode.Left:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width - lastColumnWidth, (height / 2), 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { 0.0f, (height / 2) - firstRowHeight, 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "Left", false);
		//					break;

		//				case TeachingTipPlacementMode.Right:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { firstColumnWidth, height / 2, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { 0.0f, (height / 2) - firstRowHeight, 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "Right", false);
		//					break;

		//				case TeachingTipPlacementMode.TopRight:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { firstColumnWidth + secondColumnWidth + 1, height - lastRowHeight, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { secondColumnWidth, 0.0f, 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "TopRight", false);
		//					break;

		//				case TeachingTipPlacementMode.TopLeft:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width - (nextToLastColumnWidth + lastColumnWidth + 1), height - lastRowHeight, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { width - (nextToLastColumnWidth + firstColumnWidth + lastColumnWidth), 0.0f, 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "TopLeft", false);
		//					break;

		//				case TeachingTipPlacementMode.BottomRight:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { firstColumnWidth + secondColumnWidth + 1, firstRowHeight, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { secondColumnWidth, 0.0f, 0.0f });
		//					UpdateDynamicHeroContentPlacementToBottom();
		//					VisualStateManager.GoToState(this, "BottomRight", false);
		//					break;

		//				case TeachingTipPlacementMode.BottomLeft:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width - (nextToLastColumnWidth + lastColumnWidth + 1), firstRowHeight, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { width - (nextToLastColumnWidth + firstColumnWidth + lastColumnWidth), 0.0f, 0.0f });
		//					UpdateDynamicHeroContentPlacementToBottom();
		//					VisualStateManager.GoToState(this, "BottomLeft", false);
		//					break;

		//				case TeachingTipPlacementMode.LeftTop:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width - lastColumnWidth,  height - (nextToLastRowHeight + lastRowHeight + 1), 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { 0.0f,  height - (nextToLastRowHeight + firstRowHeight + lastRowHeight), 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "LeftTop", false);
		//					break;

		//				case TeachingTipPlacementMode.LeftBottom:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width - lastColumnWidth, (firstRowHeight + secondRowHeight + 1), 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { 0.0f, secondRowHeight, 0.0f });
		//					UpdateDynamicHeroContentPlacementToBottom();
		//					VisualStateManager.GoToState(this, "LeftBottom", false);
		//					break;

		//				case TeachingTipPlacementMode.RightTop:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { firstColumnWidth, height - (nextToLastRowHeight + lastRowHeight + 1), 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { 0.0f, height - (nextToLastRowHeight + firstRowHeight + lastRowHeight), 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "RightTop", false);
		//					break;

		//				case TeachingTipPlacementMode.RightBottom:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { firstColumnWidth, (firstRowHeight + secondRowHeight + 1), 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { 0.0f, secondRowHeight, 0.0f });
		//					UpdateDynamicHeroContentPlacementToBottom();
		//					VisualStateManager.GoToState(this, "RightBottom", false);
		//					break;

		//				case TeachingTipPlacementMode.Center:
		//					TrySetCenterPoint(nullableTailOcclusionGrid, { width / 2, height - lastRowHeight, 0.0f });
		//					TrySetCenterPoint(m_tailEdgeBorder, { (width / 2) - firstColumnWidth, 0.0f, 0.0f });
		//					UpdateDynamicHeroContentPlacementToTop();
		//					VisualStateManager.GoToState(this, "Center", false);
		//					break;

		//				default:
		//					break;
		//			}

		//			return tipDoesNotFit;
		//		}

		private void PositionPopup()
		{
			bool tipDoesNotFit = false;
			if (m_target != null)
			{
				tipDoesNotFit = PositionTargetedPopup();
			}
			else
			{
				tipDoesNotFit = PositionUntargetedPopup();
			}
			if (tipDoesNotFit)
			{
				IsOpen = false;
			}

			TeachingTipTestHooks.NotifyOffsetChanged(this);
		}

		//		bool PositionTargetedPopup()
		//		{
		//			bool tipDoesNotFit = UpdateTail();
		//			var offset = PlacementMargin();

		//			var[tipHeight, tipWidth] = [this]()

		//	{
		//				if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//        {
		//					var tipHeight = tailOcclusionGrid.ActualHeight();
		//					var tipWidth = tailOcclusionGrid.ActualWidth();
		//					return Tuple.Create(tipHeight, tipWidth);
		//				}
		//				return Tuple.Create(0.0, 0.0);
		//			} ();

		//			if (var popup = m_popup)
		//    {
		//				// Depending on the effective placement mode of the tip we use a combination of the tip's size, the target's position within the app, the target's
		//				// size, and the target offset property to determine the appropriate vertical and horizontal offsets of the popup that the tip is contained in.
		//				switch (m_currentEffectiveTipPlacementMode)
		//				{
		//					case TeachingTipPlacementMode.Top:
		//						popup.VerticalOf = m_currentTargetBoundsInCoreWindowSpace.Y - tipHeight - offset.Top;
		//						popup.HorizontalOf = (((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width - tipWidth) / 2.0f);
		//						break;

		//					case TeachingTipPlacementMode.Bottom:
		//						popup.VerticalOf = m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height + (float)(offset.Bottom);
		//						popup.HorizontalOf = (((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width - tipWidth) / 2.0f);
		//						break;

		//					case TeachingTipPlacementMode.Left:
		//						popup.VerticalOf = ((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height - tipHeight) / 2.0f;
		//						popup.HorizontalOf = m_currentTargetBoundsInCoreWindowSpace.X - tipWidth - offset.Left;
		//						break;

		//					case TeachingTipPlacementMode.Right:
		//						popup.VerticalOf = ((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height - tipHeight) / 2.0f;
		//						popup.HorizontalOf = m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width + (float)(offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.TopRight:
		//						popup.VerticalOf = m_currentTargetBoundsInCoreWindowSpace.Y - tipHeight - offset.Top;
		//						popup.HorizontalOf = ((((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - MinimumTipEdgeToTailCenter());
		//						break;

		//					case TeachingTipPlacementMode.TopLeft:
		//						popup.VerticalOf = m_currentTargetBoundsInCoreWindowSpace.Y - tipHeight - offset.Top;
		//						popup.HorizontalOf = ((((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - tipWidth + MinimumTipEdgeToTailCenter());
		//						break;

		//					case TeachingTipPlacementMode.BottomRight:
		//						popup.VerticalOf = m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height + (float)(offset.Bottom);
		//						popup.HorizontalOf = ((((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - MinimumTipEdgeToTailCenter());
		//						break;

		//					case TeachingTipPlacementMode.BottomLeft:
		//						popup.VerticalOf = m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height + (float)(offset.Bottom);
		//						popup.HorizontalOf = ((((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width) / 2.0f) - tipWidth + MinimumTipEdgeToTailCenter());
		//						break;

		//					case TeachingTipPlacementMode.LeftTop:
		//						popup.VerticalOf = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - tipHeight + MinimumTipEdgeToTailCenter();
		//						popup.HorizontalOf = m_currentTargetBoundsInCoreWindowSpace.X - tipWidth - offset.Left;
		//						break;

		//					case TeachingTipPlacementMode.LeftBottom:
		//						popup.VerticalOf = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - MinimumTipEdgeToTailCenter();
		//						popup.HorizontalOf = m_currentTargetBoundsInCoreWindowSpace.X - tipWidth - offset.Left;
		//						break;

		//					case TeachingTipPlacementMode.RightTop:
		//						popup.VerticalOf = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - tipHeight + MinimumTipEdgeToTailCenter();
		//						popup.HorizontalOf = m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width + (float)(offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.RightBottom:
		//						popup.VerticalOf = (((m_currentTargetBoundsInCoreWindowSpace.Y * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Height) / 2.0f) - MinimumTipEdgeToTailCenter();
		//						popup.HorizontalOf = m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width + (float)(offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.Center:
		//						popup.VerticalOf = m_currentTargetBoundsInCoreWindowSpace.Y + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f) - tipHeight - offset.Top;
		//						popup.HorizontalOf = (((m_currentTargetBoundsInCoreWindowSpace.X * 2.0f) + m_currentTargetBoundsInCoreWindowSpace.Width - tipWidth) / 2.0f);
		//						break;

		//					default:
		//						MUX_FAIL_FAST();
		//				}
		//			}
		//			return tipDoesNotFit;
		//		}

		//		bool PositionUntargetedPopup()
		//		{
		//			var windowBoundsInCoreWindowSpace = GetEffectiveWindowBoundsInCoreWindowSpace(GetWindowBounds());

		//			var[finalTipHeight, finalTipWidth] = [this]()

		//	{
		//				if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//        {
		//					var finalTipHeight = tailOcclusionGrid.ActualHeight();
		//					var finalTipWidth = tailOcclusionGrid.ActualWidth();
		//					return Tuple.Create(finalTipHeight, finalTipWidth);
		//				}
		//				return Tuple.Create(0.0, 0.0);
		//			} ();

		//			bool tipDoesNotFit = UpdateTail();

		//			var offset = PlacementMargin();

		//			// Depending on the effective placement mode of the tip we use a combination of the tip's size, the window's size, and the target
		//			// offset property to determine the appropriate vertical and horizontal offsets of the popup that the tip is contained in.
		//			if (var popup = m_popup)
		//    {
		//				switch (PreferredPlacement())
		//				{
		//					case TeachingTipPlacementMode.Auto:
		//					case TeachingTipPlacementMode.Bottom:
		//						popup.VerticalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipCenterPlacementOffset(windowBoundsInCoreWindowSpace.X, windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Left, offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.Top:
		//						popup.VerticalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.Y, offset.Top);
		//						popup.HorizontalOf = UntargetedTipCenterPlacementOffset(windowBoundsInCoreWindowSpace.X, windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Left, offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.Left:
		//						popup.VerticalOf = UntargetedTipCenterPlacementOffset(windowBoundsInCoreWindowSpace.Y, windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Top, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.X, offset.Left);
		//						break;

		//					case TeachingTipPlacementMode.Right:
		//						popup.VerticalOf = UntargetedTipCenterPlacementOffset(windowBoundsInCoreWindowSpace.Y, windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Top, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.TopRight:
		//						popup.VerticalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.Y, offset.Top);
		//						popup.HorizontalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.TopLeft:
		//						popup.VerticalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.Y, offset.Top);
		//						popup.HorizontalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.X, offset.Left);
		//						break;

		//					case TeachingTipPlacementMode.BottomRight:
		//						popup.VerticalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.BottomLeft:
		//						popup.VerticalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.X, offset.Left);
		//						break;

		//					case TeachingTipPlacementMode.LeftTop:
		//						popup.VerticalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.Y, offset.Top);
		//						popup.HorizontalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.X, offset.Left);
		//						break;

		//					case TeachingTipPlacementMode.LeftBottom:
		//						popup.VerticalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.X, offset.Left);
		//						break;

		//					case TeachingTipPlacementMode.RightTop:
		//						popup.VerticalOf = UntargetedTipNearPlacementOffset(windowBoundsInCoreWindowSpace.Y, offset.Top);
		//						popup.HorizontalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.RightBottom:
		//						popup.VerticalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipFarPlacementOffset(windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Right);
		//						break;

		//					case TeachingTipPlacementMode.Center:
		//						popup.VerticalOf = UntargetedTipCenterPlacementOffset(windowBoundsInCoreWindowSpace.Y, windowBoundsInCoreWindowSpace.Height, finalTipHeight, offset.Top, offset.Bottom);
		//						popup.HorizontalOf = UntargetedTipCenterPlacementOffset(windowBoundsInCoreWindowSpace.X, windowBoundsInCoreWindowSpace.Width, finalTipWidth, offset.Left, offset.Right);
		//						break;

		//					default:
		//						MUX_FAIL_FAST();
		//				}
		//			}

		//			return tipDoesNotFit;
		//		}

		//		void UpdateSizeBasedTemplateSettings()
		//		{
		//			var templateSettings = TemplateSettings();
		//			var[width, height] = [this]()

		//	{
		//				if (var contentRootGrid = m_contentRootGrid)
		//        {
		//					var width = contentRootGrid.ActualWidth();
		//					var height = contentRootGrid.ActualHeight();
		//					return Tuple.Create(width, height);
		//				}
		//				return Tuple.Create(0.0, 0.0);
		//			} ();

		//			switch (m_currentEffectiveTailPlacementMode)
		//			{
		//				case TeachingTipPlacementMode.Top:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(TopEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.Bottom:
		//					templateSettings.TopRightHighlightMargin(BottomPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(BottomPlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.Left:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(LeftEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.Right:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(RightEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.TopLeft:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(TopEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.TopRight:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(TopEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.BottomLeft:
		//					templateSettings.TopRightHighlightMargin(BottomLeftPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(BottomLeftPlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.BottomRight:
		//					templateSettings.TopRightHighlightMargin(BottomRightPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(BottomRightPlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.LeftTop:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(LeftEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.LeftBottom:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(LeftEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.RightTop:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(RightEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.RightBottom:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(RightEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.Auto:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(TopEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//				case TeachingTipPlacementMode.Center:
		//					templateSettings.TopRightHighlightMargin(OtherPlacementTopRightHighlightMargin(width, height));
		//					templateSettings.TopLeftHighlightMargin(TopEdgePlacementTopLeftHighlightMargin(width, height));
		//					break;
		//			}
		//		}

		//		void UpdateButtonsState()
		//		{
		//			var actionContent = ActionButtonContent();
		//			var closeContent = CloseButtonContent();
		//			bool isLightDismiss = IsLightDismissEnabled();
		//			if (actionContent && closeContent)
		//			{
		//				VisualStateManager.GoToState(this, "BothButtonsVisible", false);
		//				VisualStateManager.GoToState(this, "FooterCloseButton", false);
		//			}
		//			else if (actionContent && isLightDismiss)
		//			{
		//				VisualStateManager.GoToState(this, "ActionButtonVisible", false);
		//				VisualStateManager.GoToState(this, "FooterCloseButton", false);
		//			}
		//			else if (actionContent)
		//			{
		//				VisualStateManager.GoToState(this, "ActionButtonVisible", false);
		//				VisualStateManager.GoToState(this, "HeaderCloseButton", false);
		//			}
		//			else if (closeContent)
		//			{
		//				VisualStateManager.GoToState(this, "CloseButtonVisible", false);
		//				VisualStateManager.GoToState(this, "FooterCloseButton", false);
		//			}
		//			else if (isLightDismiss)
		//			{
		//				VisualStateManager.GoToState(this, "NoButtonsVisible", false);
		//				VisualStateManager.GoToState(this, "FooterCloseButton", false);
		//			}
		//			else
		//			{
		//				VisualStateManager.GoToState(this, "NoButtonsVisible", false);
		//				VisualStateManager.GoToState(this, "HeaderCloseButton", false);
		//			}
		//		}

		//		void UpdateDynamicHeroContentPlacementToTop()
		//		{
		//			if (HeroContentPlacement() == TeachingTipHeroContentPlacementMode.Auto)
		//			{
		//				VisualStateManager.GoToState(this, "HeroContentTop", false);
		//				if (m_currentHeroContentEffectivePlacementMode != TeachingTipHeroContentPlacementMode.Top)
		//				{
		//					m_currentHeroContentEffectivePlacementMode = TeachingTipHeroContentPlacementMode.Top;
		//					TeachingTipTestHooks.NotifyEffectiveHeroContentPlacementChanged(this);
		//				}
		//			}
		//		}

		//		void UpdateDynamicHeroContentPlacementToBottom()
		//		{
		//			if (HeroContentPlacement() == TeachingTipHeroContentPlacementMode.Auto)
		//			{
		//				VisualStateManager.GoToState(this, "HeroContentBottom", false);
		//				if (m_currentHeroContentEffectivePlacementMode != TeachingTipHeroContentPlacementMode.Bottom)
		//				{
		//					m_currentHeroContentEffectivePlacementMode = TeachingTipHeroContentPlacementMode.Bottom;
		//					TeachingTipTestHooks.NotifyEffectiveHeroContentPlacementChanged(this);
		//				}
		//			}
		//		}

		private void OnIsOpenChanged()
		{
			SharedHelpers.QueueCallbackForCompositionRendering([strongThis = get_strong()]()

			{
				if (strongThis.IsOpen())
				{
					strongThis.IsOpenChangedToOpen();
				}
				else
				{
					strongThis.IsOpenChangedToClose();
				}
				TeachingTipTestHooks.NotifyOpenedStatusChanged(*strongThis);
			});
		}

		//		void IsOpenChangedToOpen()
		//		{
		//			//Reset the close reason to the default value of programmatic.
		//			m_lastCloseReason = TeachingTipCloseReason.Programmatic;

		//			m_currentBoundsInCoreWindowSpace = this.TransformToVisual(null).TransformBounds({
		//				0.0,
		//        0.0,
		//        (float)(this.ActualWidth()),
		//        (float)(this.ActualHeight())

		//		});

		//			m_currentTargetBoundsInCoreWindowSpace = [this]()

		//	{
		//				if (var target = m_target)
		//        {
		//					SetViewportChangedEvent(gsl.make_strict_not_null(target));
		//					return target.TransformToVisual(null).TransformBounds({
		//						0.0,
		//                0.0,
		//                (float)(target.as< FrameworkElement > ().ActualWidth()),
		//                (float)(target.as< FrameworkElement > ().ActualHeight())

		//				});
		//				}
		//				return Rect{ 0,0,0,0 };
		//			} ();

		//			if (!m_lightDismissIndicatorPopup)
		//			{
		//				CreateLightDismissIndicatorPopup();
		//			}
		//			OnIsLightDismissEnabledChanged();

		//			if (!m_contractAnimation)
		//			{
		//				CreateContractAnimation();
		//			}
		//			if (!m_expandAnimation)
		//			{
		//				CreateExpandAnimation();
		//			}

		//			// We are about to begin the process of trying to open the teaching tip, so notify that we are no longer idle.
		//			SetIsIdle(false);

		//			//If the developer defines their TeachingTip in a resource dictionary it is possible that it's template will have never been applied
		//			if (!m_isTemplateApplied)
		//			{
		//				this.ApplyTemplate();
		//			}

		//			if (!m_popup || m_createNewPopupOnOpen)
		//			{
		//				CreateNewPopup();
		//			}

		//			// If the tip is not going to open because it does not fit we need to make sure that
		//			// the open, closing, closed life cycle still fires so that we don't cause apps to leak
		//			// that depend on this sequence.
		//			var[ignored, tipDoesNotFit] = DetermineEffectivePlacement();
		//			if (tipDoesNotFit)
		//			{
		//				__RP_Marker_ClassMemberById(RuntimeProfiler.ProfId_TeachingTip, RuntimeProfiler.ProfMemberId_TeachingTip_TipDidNotOpenDueToSize);
		//				RaiseClosingEvent(false);
		//				var closedArgs = make_self<TeachingTipClosedEventArgs>();
		//				closedArgs.Reason(m_lastCloseReason);
		//				m_closedEventSource(this, *closedArgs);
		//				IsOpen(false);
		//			}
		//			else
		//			{
		//				if (var popup = m_popup)
		//        {
		//					if (!popup.IsOpen())
		//					{
		//						UpdatePopupRequestedTheme();
		//						popup.Child(m_rootElement);
		//						if (var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup)
		//                {
		//							lightDismissIndicatorPopup.IsOpen(true);
		//						}
		//						popup.IsOpen(true);
		//					}
		//					else
		//					{
		//						// We have become Open but our popup was already open. This can happen when a close is canceled by the closing event, so make sure the idle status is correct.
		//						if (!m_isExpandAnimationPlaying && !m_isContractAnimationPlaying)
		//						{
		//							SetIsIdle(true);
		//						}
		//					}
		//				}
		//			}

		//			m_acceleratorKeyActivatedRevoker = Dispatcher().AcceleratorKeyActivated(auto_revoke, { this, &TeachingTip.OnF6AcceleratorKeyClicked });

		//			// Make sure we are in the correct VSM state after ApplyTemplate and moving the template content from the Control to the Popup:
		//			OnIsLightDismissEnabledChanged();
		//		}

		//		void IsOpenChangedToClose()
		//		{
		//			if (var popup = m_popup)
		//    {
		//				if (popup.IsOpen())
		//				{
		//					// We are about to begin the process of trying to close the teaching tip, so notify that we are no longer idle.
		//					SetIsIdle(false);
		//					RaiseClosingEvent(true);
		//				}
		//				else
		//				{
		//					// We have become not Open but our popup was already not open. Lets make sure the idle status is correct.
		//					if (!m_isExpandAnimationPlaying && !m_isContractAnimationPlaying)
		//					{
		//						SetIsIdle(true);
		//					}
		//				}
		//			}

		//			m_currentEffectiveTipPlacementMode = TeachingTipPlacementMode.Auto;
		//			TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
		//		}

		//		void CreateNewPopup()
		//		{
		//			m_popupOpenedRevoker.revoke();
		//			m_popupClosedRevoker.revoke();

		//			var popup = Popup();
		//			// Set XamlRoot on the popup to handle XamlIsland/AppWindow scenarios.
		//			if (UIElement10 uiElement10 = this)
		//    {
		//				popup.XamlRoot(uiElement10.XamlRoot());
		//			}

		//			m_popupOpenedRevoker = popup.Opened(auto_revoke, { this, &TeachingTip.OnPopupOpened });
		//			m_popupClosedRevoker = popup.Closed(auto_revoke, { this, &TeachingTip.OnPopupClosed });
		//			if (IPopup3 popup3 = popup)
		//    {
		//				popup3.ShouldConstrainToRootBounds(ShouldConstrainToRootBounds());
		//			}
		//			m_popup = popup;
		//			SetPopupAutomationProperties();
		//			m_createNewPopupOnOpen = false;
		//		}

		//		void OnTailVisibilityChanged()
		//		{
		//			UpdateTail();
		//		}

		//		void OnIconSourceChanged()
		//		{
		//			var templateSettings = TemplateSettings();
		//			if (var source = IconSource())
		//    {
		//				templateSettings.IconElement(SharedHelpers.MakeIconElementFrom(source));
		//				VisualStateManager.GoToState(this, "Icon", false);
		//			}

		//	else
		//			{
		//				templateSettings.IconElement(null);
		//				VisualStateManager.GoToState(this, "NoIcon", false);
		//			}
		//		}

		//		void OnPlacementMarginChanged()
		//		{
		//			if (IsOpen())
		//			{
		//				PositionPopup();
		//			}
		//		}

		//		void OnIsLightDismissEnabledChanged()
		//		{
		//			if (IsLightDismissEnabled())
		//			{
		//				VisualStateManager.GoToState(this, "LightDismiss", false);
		//				if (var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup)
		//        {
		//					lightDismissIndicatorPopup.IsLightDismissEnabled(true);
		//					m_lightDismissIndicatorPopupClosedRevoker = lightDismissIndicatorPopup.Closed(auto_revoke, { this, &TeachingTip.OnLightDismissIndicatorPopupClosed });
		//				}
		//			}
		//			else
		//			{
		//				VisualStateManager.GoToState(this, "NormalDismiss", false);
		//				if (var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup)
		//        {
		//					lightDismissIndicatorPopup.IsLightDismissEnabled(false);
		//				}
		//				m_lightDismissIndicatorPopupClosedRevoker.revoke();
		//			}
		//			UpdateButtonsState();
		//		}

		//		void OnShouldConstrainToRootBoundsChanged()
		//		{
		//			// ShouldConstrainToRootBounds is a property that can only be set on a popup before it is opened.
		//			// If we have opened the tip's popup and then this property changes we will need to discard the old popup
		//			// and replace it with a new popup.  This variable indicates this state.

		//			//The underlying popup api is only available on 19h1 plus, if we aren't on that no opt.
		//			if (m_popup as Controls.Primitives.IPopup3())
		//    {
		//				m_createNewPopupOnOpen = true;
		//			}
		//		}

		//		void OnHeroContentPlacementChanged()
		//		{
		//			switch (HeroContentPlacement())
		//			{
		//				case TeachingTipHeroContentPlacementMode.Auto:
		//					break;
		//				case TeachingTipHeroContentPlacementMode.Top:
		//					VisualStateManager.GoToState(this, "HeroContentTop", false);
		//					if (m_currentHeroContentEffectivePlacementMode != TeachingTipHeroContentPlacementMode.Top)
		//					{
		//						m_currentHeroContentEffectivePlacementMode = TeachingTipHeroContentPlacementMode.Top;
		//						TeachingTipTestHooks.NotifyEffectiveHeroContentPlacementChanged(this);
		//					}
		//					break;
		//				case TeachingTipHeroContentPlacementMode.Bottom:
		//					VisualStateManager.GoToState(this, "HeroContentBottom", false);
		//					if (m_currentHeroContentEffectivePlacementMode != TeachingTipHeroContentPlacementMode.Bottom)
		//					{
		//						m_currentHeroContentEffectivePlacementMode = TeachingTipHeroContentPlacementMode.Bottom;
		//						TeachingTipTestHooks.NotifyEffectiveHeroContentPlacementChanged(this);
		//					}
		//					break;
		//			}

		//			// Setting m_currentEffectiveTipPlacementMode to var ensures that the next time position popup is called we'll rerun the DetermineEffectivePlacement
		//			// algorithm. If we did not do this and the popup was opened the algorithm would maintain the current effective placement mode, which we don't want
		//			// since the hero content placement contributes to the choice of tip placement mode.
		//			m_currentEffectiveTipPlacementMode = TeachingTipPlacementMode.Auto;
		//			TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
		//			if (IsOpen())
		//			{
		//				PositionPopup();
		//			}
		//		}

		//		void OnContentSizeChanged(object&, SizeChangedEventArgs& args)
		//		{
		//			UpdateSizeBasedTemplateSettings();
		//			// Reset the currentEffectivePlacementMode so that the tail will be updated for the new size as well.
		//			m_currentEffectiveTipPlacementMode = TeachingTipPlacementMode.Auto;
		//			TeachingTipTestHooks.NotifyEffectivePlacementChanged(this);
		//			if (IsOpen())
		//			{
		//				PositionPopup();
		//			}
		//			if (var expandAnimation = m_expandAnimation)
		//    {
		//				expandAnimation.SetScalarParameter("Width", args.NewSize().Width);
		//				expandAnimation.SetScalarParameter("Height", args.NewSize().Height);
		//			}
		//			if (var contractAnimation = m_contractAnimation)
		//    {
		//				contractAnimation.SetScalarParameter("Width", args.NewSize().Width);
		//				contractAnimation.SetScalarParameter("Height", args.NewSize().Height);
		//			}
		//		}

		//		void TeachingTip.OnF6AcceleratorKeyClicked(CoreDispatcher&, AcceleratorKeyEventArgs& args)
		//		{
		//			if (!args.Handled() &&
		//				IsOpen() &&
		//				args.VirtualKey() == VirtualKey.F6 &&
		//				args.EventType() == CoreAcceleratorKeyEventType.KeyDown)
		//			{
		//				//  Logging usage telemetry
		//				if (m_hasF6BeenInvoked)
		//				{
		//					__RP_Marker_ClassMemberById(RuntimeProfiler.ProfId_TeachingTip, RuntimeProfiler.ProfMemberId_TeachingTip_F6AccessKey_SubsequentInvocation);
		//				}
		//				else
		//				{
		//					__RP_Marker_ClassMemberById(RuntimeProfiler.ProfId_TeachingTip, RuntimeProfiler.ProfMemberId_TeachingTip_F6AccessKey_FirstInvocation);
		//					m_hasF6BeenInvoked = true;
		//				}

		//				var hasFocusInSubtree = [this, args]()

		//		{
		//					var current = FocusManager.GetFocusedElement() as DependencyObject();
		//					if (var rootElement = m_rootElement)
		//            {
		//						while (current)
		//						{
		//							if (current as UIElement() == rootElement)
		//                    {
		//								return true;
		//							}
		//							current = VisualTreeHelper.GetParent(current);
		//						}
		//					}
		//					return false;
		//				} ();

		//				if (hasFocusInSubtree)
		//				{
		//					bool setFocus = SetFocus(m_previouslyFocusedElement, FocusState.Programmatic);
		//					m_previouslyFocusedElement = null;
		//					args.Handled(setFocus);
		//				}
		//				else
		//				{
		//					Button f6Button = [this]().Button

		//			{
		//						var firstButton = m_closeButton;
		//						var secondButton = m_alternateCloseButton;
		//						//Prefer the close button to the alternate, except when there is no content.
		//						if (!CloseButtonContent())
		//						{
		//							std.swap(firstButton, secondButton);
		//						}
		//						if (firstButton && firstButton.Visibility() == Visibility.Visible)
		//						{
		//							return firstButton;
		//						}
		//						else if (secondButton && secondButton.Visibility() == Visibility.Visible)
		//						{
		//							return secondButton;
		//						}
		//						return null;
		//					} ();

		//					if (f6Button)
		//					{
		//						var scopedRevoker = f6Button.GettingFocus(auto_revoke, [this](var &, var args) {
		//							m_previouslyFocusedElement = make_weak(args.OldFocusedElement());
		//						});
		//						bool setFocus = f6Button.Focus(FocusState.Keyboard);
		//						args.Handled(setFocus);
		//					}
		//				}
		//			}
		//		}

		//		void OnAutomationNameChanged(object&, object&)
		//		{
		//			SetPopupAutomationProperties();
		//		}

		//		void OnAutomationIdChanged(object&, object&)
		//		{
		//			SetPopupAutomationProperties();
		//		}

		//		void OnCloseButtonClicked(object&, RoutedEventArgs&)
		//		{
		//			m_closeButtonClickEventSource(this, null);
		//			m_lastCloseReason = TeachingTipCloseReason.CloseButton;
		//			IsOpen(false);
		//		}

		//		void OnActionButtonClicked(object&, RoutedEventArgs&)
		//		{
		//			m_actionButtonClickEventSource(this, null);
		//		}

		//		void OnPopupOpened(object&, object&)
		//		{
		//			if (UIElement10 uiElement10 = this)
		//    {
		//				if (var xamlRoot = uiElement10.XamlRoot())
		//        {
		//					m_currentXamlRootSize = xamlRoot.Size();
		//					m_xamlRoot = xamlRoot;
		//					m_xamlRootChangedRevoker = xamlRoot.Changed(auto_revoke, { this, &TeachingTip.XamlRootChanged });
		//				}
		//			}

		//	else
		//			{
		//				if (var coreWindow = CoreWindow.GetForCurrentThread())
		//        {
		//					m_windowSizeChangedRevoker = coreWindow.SizeChanged(auto_revoke, { this, &TeachingTip.WindowSizeChanged });
		//				}
		//			}

		//			// Expand animation requires UIElement9
		//			if (this as UIElement9() && SharedHelpers.IsAnimationsEnabled())
		//    {
		//				StartExpandToOpen();
		//			}

		//	else
		//			{
		//				// We won't be playing an animation so we're immediately idle.
		//				SetIsIdle(true);
		//			}

		//			if (var teachingTipPeer = FrameworkElementAutomationPeer.FromElement(this) as TeachingTipAutomationPeer())
		//    {
		//				var notificationString = [this]()

		//		{
		//					var appName = []()

		//			{
		//						try
		//						{
		//							if (var package = ApplicationModel.Package.Current())
		//                    {
		//								return package.DisplayName();
		//							}
		//						}
		//						catch { }

		//						return hstring{ };
		//					} ();

		//					if (!appName.empty())
		//					{
		//						return StringUtil.FormatString(
		//							ResourceAccessor.GetLocalizedStringResource(SR_TeachingTipNotification),
		//							appName.data(),
		//							AutomationProperties.GetName(m_popup).data());
		//					}
		//					else
		//					{
		//						return StringUtil.FormatString(
		//							ResourceAccessor.GetLocalizedStringResource(SR_TeachingTipNotificationWithoutAppName),
		//							AutomationProperties.GetName(m_popup).data());
		//					}
		//				} ();

		//				get_self<TeachingTipAutomationPeer>(teachingTipPeer).RaiseWindowOpenedEvent(notificationString);
		//			}
		//		}

		//		void OnPopupClosed(object&, object&)
		//		{
		//			m_windowSizeChangedRevoker.revoke();
		//			m_xamlRootChangedRevoker.revoke();
		//			m_xamlRoot = null;
		//			if (var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup)
		//    {
		//				lightDismissIndicatorPopup.IsOpen(false);
		//			}
		//			if (var popup = m_popup)
		//    {
		//				popup.Child(null);
		//			}
		//			var myArgs = make_self<TeachingTipClosedEventArgs>();

		//			myArgs.Reason(m_lastCloseReason);
		//			m_closedEventSource(this, *myArgs);

		//			//If we were closed by the close button and we have tracked a previously focused element because F6 was used
		//			//To give the tip focus, then we return focus when the popup closes.
		//			if (m_lastCloseReason == TeachingTipCloseReason.CloseButton)
		//			{
		//				SetFocus(m_previouslyFocusedElement, FocusState.Programmatic);
		//			}
		//			m_previouslyFocusedElement = null;

		//			if (var teachingTipPeer = FrameworkElementAutomationPeer.FromElement(this) as TeachingTipAutomationPeer())
		//    {
		//				get_self<TeachingTipAutomationPeer>(teachingTipPeer).RaiseWindowClosedEvent();
		//			}
		//		}

		private void ClosePopupOnUnloadEvent(object sender, RoutedEventArgs args)
		{
			IsOpen = false;
			ClosePopup();
		}

		//		void OnLightDismissIndicatorPopupClosed(object&, object&)
		//		{
		//			if (IsOpen())
		//			{
		//				m_lastCloseReason = TeachingTipCloseReason.LightDismiss;
		//			}
		//			IsOpen(false);
		//		}

		//		void RaiseClosingEvent(bool attachDeferralCompletedHandler)
		//		{
		//			var args = make_self<TeachingTipClosingEventArgs>();
		//			args.Reason(m_lastCloseReason);

		//			if (attachDeferralCompletedHandler)
		//			{
		//				Deferral instance{
		//					[strongThis = get_strong(), args]()

		//			{
		//						strongThis.CheckThread();
		//						if (!args.Cancel())
		//						{
		//							strongThis.ClosePopupWithAnimationIfAvailable();
		//						}
		//						else
		//						{
		//							// The developer has changed the Cancel property to true, indicating that they wish to Cancel the
		//							// closing of this tip, so we need to revert the IsOpen property to true.
		//							strongThis.IsOpen(true);
		//						}
		//					}
		//				};

		//				args.SetDeferral(instance);

		//				args.IncrementDeferralCount();
		//				m_closingEventSource(this, *args);
		//				args.DecrementDeferralCount();
		//			}
		//			else
		//			{
		//				m_closingEventSource(this, *args);
		//			}
		//		}

		//		void ClosePopupWithAnimationIfAvailable()
		//		{
		//			if (m_popup && m_popup.IsOpen())
		//			{
		//				// Contract animation requires UIElement9
		//				if (this as UIElement9() && SharedHelpers.IsAnimationsEnabled())
		//        {
		//					StartContractToClose();
		//				}

		//		else
		//				{
		//					ClosePopup();
		//				}

		//				// Under normal circumstances we would have launched an animation just now, if we did not then we should make sure
		//				// that the idle state is correct.
		//				if (!m_isContractAnimationPlaying && !m_isExpandAnimationPlaying)
		//				{
		//					SetIsIdle(true);
		//				}
		//			}
		//		}

		//		void ClosePopup()
		//		{
		//			if (var popup = m_popup)
		//    {
		//				popup.IsOpen(false);
		//			}
		//			if (var lightDismissIndicatorPopup = m_lightDismissIndicatorPopup)
		//    {
		//				lightDismissIndicatorPopup.IsOpen(false);
		//			}
		//			if (UIElement9  tailOcclusionGrid = m_tailOcclusionGrid)
		//    {
		//				// A previous close animation may have left the rootGrid's scale at a very small value and if this teaching tip
		//				// is shown again then its text would be rasterized at this small scale and blown up ~20x. To fix this we have to
		//				// reset the scale after the popup has closed so that if the teaching tip is re-shown the render pass does not use the
		//				// small scale.
		//				tailOcclusionGrid.Scale({ 1.0f,1.0f,1.0f });
		//			}
		//		}

		//		void OnTargetChanged()
		//		{
		//			m_targetLayoutUpdatedRevoker.revoke();
		//			m_targetEffectiveViewportChangedRevoker.revoke();
		//			m_targetLoadedRevoker.revoke();

		//			var target = Target();
		//			m_target = target;

		//			if (target)
		//			{
		//				m_targetLoadedRevoker = target.Loaded(auto_revoke, { this, &TeachingTip.OnTargetLoaded });
		//			}

		//			if (IsOpen())
		//			{
		//				if (target)
		//				{
		//					m_currentTargetBoundsInCoreWindowSpace = target.TransformToVisual(null).TransformBounds({
		//						0.0,
		//                0.0,
		//                (float)(target.ActualWidth()),
		//                (float)(target.ActualHeight())

		//			});
		//					SetViewportChangedEvent(gsl.make_strict_not_null(target));
		//				}
		//				PositionPopup();
		//			}
		//		}

		//		void SetViewportChangedEvent(gsl.strict_not_null<FrameworkElement>& target)
		//		{
		//			if (m_tipFollowsTarget)
		//			{
		//				// EffectiveViewPortChanged is only available on RS5 and higher.
		//				if (FrameworkElement7 targetAsFE7 = target)
		//        {
		//					m_targetEffectiveViewportChangedRevoker = targetAsFE7.EffectiveViewportChanged(auto_revoke, { this, &TeachingTip.OnTargetLayoutUpdated });
		//					m_effectiveViewportChangedRevoker = this.EffectiveViewportChanged(auto_revoke, { this, &TeachingTip.OnTargetLayoutUpdated });
		//				}

		//		else
		//				{
		//					m_targetLayoutUpdatedRevoker = target.LayoutUpdated(auto_revoke, { this, &TeachingTip.OnTargetLayoutUpdated });
		//				}
		//			}
		//		}

		//		void RevokeViewportChangedEvent()
		//		{
		//			m_targetEffectiveViewportChangedRevoker.revoke();
		//			m_effectiveViewportChangedRevoker.revoke();
		//			m_targetLayoutUpdatedRevoker.revoke();
		//		}

		//		void WindowSizeChanged(CoreWindow&, WindowSizeChangedEventArgs&)
		//		{
		//			// Reposition popup when target/window has finished determining sizes
		//			SharedHelpers.QueueCallbackForCompositionRendering(

		//				[strongThis = get_strong()](){
		//				strongThis.RepositionPopup();
		//			}
		//    );
		//		}

		//		void XamlRootChanged(XamlRoot& xamlRoot, XamlRootChangedEventArgs&)
		//		{
		//			// Reposition popup when target has finished determining its own position.
		//			SharedHelpers.QueueCallbackForCompositionRendering(

		//				[strongThis = get_strong(), xamlRootSize = xamlRoot.Size()](){
		//				if (xamlRootSize != strongThis.m_currentXamlRootSize)
		//				{
		//					strongThis.m_currentXamlRootSize = xamlRootSize;
		//					strongThis.RepositionPopup();
		//				}
		//			}
		//    );

		//		}

		//		void RepositionPopup()
		//		{
		//			if (IsOpen())
		//			{
		//				var newTargetBounds = [this]()

		//		{
		//					if (var target = m_target)
		//            {
		//						return target.TransformToVisual(null).TransformBounds({
		//							0.0,
		//                    0.0,
		//                    (float)(target.as< FrameworkElement > ().ActualWidth()),
		//                    (float)(target.as< FrameworkElement > ().ActualHeight())

		//					});
		//					}
		//					return Rect{ };
		//				} ();

		//				var newCurrentBounds = this.TransformToVisual(null).TransformBounds({
		//					0.0,
		//            0.0,
		//            (float)(this.ActualWidth()),
		//            (float)(this.ActualHeight())

		//			});

		//				if (newTargetBounds != m_currentTargetBoundsInCoreWindowSpace || newCurrentBounds != m_currentBoundsInCoreWindowSpace)
		//				{
		//					m_currentBoundsInCoreWindowSpace = newCurrentBounds;
		//					m_currentTargetBoundsInCoreWindowSpace = newTargetBounds;
		//					PositionPopup();
		//				}
		//			}
		//		}

		//		void OnTargetLoaded(object&, object&)
		//		{
		//			RepositionPopup();
		//		}

		//		void OnTargetLayoutUpdated(object&, object&)
		//		{
		//			RepositionPopup();
		//		}

		//		void CreateExpandAnimation()
		//		{
		//			var compositor = Window.Current().Compositor();

		//			var expandEasingFunction = [this, compositor]()

		//	{
		//				if (!m_expandEasingFunction)
		//				{
		//					var easingFunction = compositor.CreateCubicBezierEasingFunction(s_expandAnimationEasingCurveControlPoint1, s_expandAnimationEasingCurveControlPoint2);
		//					m_expandEasingFunction = easingFunction;
		//					return (CompositionEasingFunction)(easingFunction);
		//				}
		//				return m_expandEasingFunction;
		//			} ();

		//			m_expandAnimation = [this, compositor, expandEasingFunction](

		//	{
		//				var expandAnimation = compositor.CreateVector3KeyFrameAnimation();
		//				if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//        {
		//					expandAnimation.SetScalarParameter("Width", (float)(tailOcclusionGrid.ActualWidth()));
		//					expandAnimation.SetScalarParameter("Height", (float)(tailOcclusionGrid.ActualHeight()));
		//				}

		//		else
		//				{
		//					expandAnimation.SetScalarParameter("Width", s_defaultTipHeightAndWidth);
		//					expandAnimation.SetScalarParameter("Height", s_defaultTipHeightAndWidth);
		//				}

		//				expandAnimation.InsertExpressionKeyFrame(0.0f, "Vector3(Min(0.01, 20.0 / Width), Min(0.01, 20.0 / Height), 1.0)");
		//				expandAnimation.InsertKeyFrame(1.0f, { 1.0f, 1.0f, 1.0f }, expandEasingFunction);
		//				expandAnimation.Duration(m_expandAnimationDuration);
		//				expandAnimation.Target(s_scaleTargetName);
		//				return expandAnimation;
		//			} ());

		//			m_expandElevationAnimation = [this, compositor, expandEasingFunction](

		//	{
		//				var expandElevationAnimation = compositor.CreateVector3KeyFrameAnimation();
		//				expandElevationAnimation.InsertExpressionKeyFrame(1.0f, "Vector3(this.Target.Translation.X, this.Target.Translation.Y, contentElevation)", expandEasingFunction);
		//				expandElevationAnimation.SetScalarParameter("contentElevation", m_contentElevation);
		//				expandElevationAnimation.Duration(m_expandAnimationDuration);
		//				expandElevationAnimation.Target(s_translationTargetName);
		//				return expandElevationAnimation;
		//			} ());
		//		}

		//		void CreateContractAnimation()
		//		{
		//			var compositor = Window.Current().Compositor();

		//			var contractEasingFunction = [this, compositor]()

		//	{
		//				if (!m_contractEasingFunction)
		//				{
		//					var easingFunction = compositor.CreateCubicBezierEasingFunction(s_contractAnimationEasingCurveControlPoint1, s_contractAnimationEasingCurveControlPoint2);
		//					m_contractEasingFunction = easingFunction;
		//					return (CompositionEasingFunction)(easingFunction);
		//				}
		//				return m_contractEasingFunction;
		//			} ();

		//			m_contractAnimation = [this, compositor, contractEasingFunction](

		//	{
		//				var contractAnimation = compositor.CreateVector3KeyFrameAnimation();
		//				if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//        {
		//					contractAnimation.SetScalarParameter("Width", (float)(tailOcclusionGrid.ActualWidth()));
		//					contractAnimation.SetScalarParameter("Height", (float)(tailOcclusionGrid.ActualHeight()));
		//				}

		//		else
		//				{
		//					contractAnimation.SetScalarParameter("Width", s_defaultTipHeightAndWidth);
		//					contractAnimation.SetScalarParameter("Height", s_defaultTipHeightAndWidth);
		//				}

		//				contractAnimation.InsertKeyFrame(0.0f, { 1.0f, 1.0f, 1.0f });
		//				contractAnimation.InsertExpressionKeyFrame(1.0f, "Vector3(20.0 / Width, 20.0 / Height, 1.0)", contractEasingFunction);
		//				contractAnimation.Duration(m_contractAnimationDuration);
		//				contractAnimation.Target(s_scaleTargetName);
		//				return contractAnimation;
		//			} ());

		//			m_contractElevationAnimation = [this, compositor, contractEasingFunction](

		//	{
		//				var contractElevationAnimation = compositor.CreateVector3KeyFrameAnimation();
		//				// animating to 0.01f instead of 0.0f as work around to internal issue 26001712 which was causing text clipping.
		//				contractElevationAnimation.InsertExpressionKeyFrame(1.0f, "Vector3(this.Target.Translation.X, this.Target.Translation.Y, 0.01f)", contractEasingFunction);
		//				contractElevationAnimation.Duration(m_contractAnimationDuration);
		//				contractElevationAnimation.Target(s_translationTargetName);
		//				return contractElevationAnimation;
		//			} ());
		//		}

		//		void StartExpandToOpen()
		//		{
		//			MUX_ASSERT_MSG(this as UIElement9(), "The contract and expand animations currently use facade's which were not available pre-RS5.");
		//			if (!m_expandAnimation)
		//			{
		//				CreateExpandAnimation();
		//			}

		//			var scopedBatch = [this]()

		//	{
		//				var scopedBatch = Window.Current().Compositor().CreateScopedBatch(CompositionBatchTypes.Animation);

		//				if (var expandAnimation = m_expandAnimation)
		//        {
		//					if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//            {
		//						tailOcclusionGrid.StartAnimation(expandAnimation);
		//						m_isExpandAnimationPlaying = true;
		//					}
		//					if (var tailEdgeBorder = m_tailEdgeBorder)
		//            {
		//						tailEdgeBorder.StartAnimation(expandAnimation);
		//						m_isExpandAnimationPlaying = true;
		//					}
		//				}
		//				if (var expandElevationAnimation = m_expandElevationAnimation)
		//        {
		//					if (var contentRootGrid = m_contentRootGrid)
		//            {
		//						contentRootGrid.StartAnimation(expandElevationAnimation);
		//						m_isExpandAnimationPlaying = true;
		//					}
		//				}
		//				return scopedBatch;
		//			} ();
		//			scopedBatch.End();

		//			scopedBatch.Completed([strongThis = get_strong()](auto, auto)

		//	{
		//				strongThis.m_isExpandAnimationPlaying = false;
		//				if (!strongThis.m_isContractAnimationPlaying)
		//				{
		//					strongThis.SetIsIdle(true);
		//				}
		//			});

		//			// Under normal circumstances we would have launched an animation just now, if we did not then we should make sure that the idle state is correct
		//			if (!m_isExpandAnimationPlaying && !m_isContractAnimationPlaying)
		//			{
		//				SetIsIdle(true);
		//			}
		//		}

		//		void StartContractToClose()
		//		{
		//			MUX_ASSERT_MSG(this as UIElement9(), "The contract and expand animations currently use facade's which were not available pre RS5.");
		//			if (!m_contractAnimation)
		//			{
		//				CreateContractAnimation();
		//			}

		//			var scopedBatch = [this]()

		//	{
		//				var scopedBatch = Window.Current().Compositor().CreateScopedBatch(CompositionBatchTypes.Animation);
		//				if (var contractAnimation = m_contractAnimation)
		//        {
		//					if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//            {
		//						tailOcclusionGrid.StartAnimation(contractAnimation);
		//						m_isContractAnimationPlaying = true;
		//					}
		//					if (var tailEdgeBorder = m_tailEdgeBorder)
		//            {
		//						tailEdgeBorder.StartAnimation(contractAnimation);
		//						m_isContractAnimationPlaying = true;
		//					}
		//				}
		//				if (var contractElevationAnimation = m_contractElevationAnimation)
		//        {
		//					if (var contentRootGrid = m_contentRootGrid)
		//            {
		//						contentRootGrid.StartAnimation(contractElevationAnimation);
		//						m_isContractAnimationPlaying = true;
		//					}
		//				}
		//				return scopedBatch;
		//			} ();
		//			scopedBatch.End();

		//			scopedBatch.Completed([strongThis = get_strong()](auto, auto)

		//	{
		//				strongThis.m_isContractAnimationPlaying = false;
		//				strongThis.ClosePopup();
		//				if (!strongThis.m_isExpandAnimationPlaying)
		//				{
		//					strongThis.SetIsIdle(true);
		//				}
		//			});
		//		}

		//		Tuple<TeachingTipPlacementMode, bool> DetermineEffectivePlacement()
		//		{
		//			// Because we do not have access to APIs to give us details about multi monitor scenarios we do not have the ability to correctly
		//			// Place the tip in scenarios where we have an out of root bounds tip. Since this is the case we have decided to do no special
		//			// calculations and return the provided value or top if var was set. This behavior can be removed via the
		//			// SetReturnTopForOutOfWindowBounds test hook.
		//			if (!ShouldConstrainToRootBounds() && m_returnTopForOutOfWindowPlacement)
		//			{
		//				var placement = PreferredPlacement();
		//				if (placement == TeachingTipPlacementMode.Auto)
		//				{
		//					return Tuple.Create(TeachingTipPlacementMode.Top, false);
		//				}
		//				return Tuple.Create(placement, false);
		//			}

		//			if (IsOpen() && m_currentEffectiveTipPlacementMode != TeachingTipPlacementMode.Auto)
		//			{
		//				return Tuple.Create(m_currentEffectiveTipPlacementMode, false);
		//			}

		//			var[contentHeight, contentWidth] = [this]()

		//	{
		//				if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//        {
		//					double contentHeight = tailOcclusionGrid.ActualHeight();
		//					double contentWidth = tailOcclusionGrid.ActualWidth();
		//					return Tuple.Create(contentHeight, contentWidth);
		//				}
		//				return Tuple.Create(0.0, 0.0);
		//			} ();

		//			if (m_target)
		//			{
		//				return DetermineEffectivePlacementTargeted(contentHeight, contentWidth);
		//			}
		//			else
		//			{
		//				return DetermineEffectivePlacementUntargeted(contentHeight, contentWidth);
		//			}
		//		}

		//		Tuple<TeachingTipPlacementMode, bool> DetermineEffectivePlacementTargeted(double contentHeight, double contentWidth)
		//		{
		//			// These variables will track which positions the tip will fit in. They all start true and are
		//			// flipped to false when we find a display condition that is not met.
		//			enum_array < TeachingTipPlacementMode, bool, 14 > availability;
		//			availability[TeachingTipPlacementMode.Auto] = false;
		//			availability[TeachingTipPlacementMode.Top] = true;
		//			availability[TeachingTipPlacementMode.Bottom] = true;
		//			availability[TeachingTipPlacementMode.Right] = true;
		//			availability[TeachingTipPlacementMode.Left] = true;
		//			availability[TeachingTipPlacementMode.TopLeft] = true;
		//			availability[TeachingTipPlacementMode.TopRight] = true;
		//			availability[TeachingTipPlacementMode.BottomLeft] = true;
		//			availability[TeachingTipPlacementMode.BottomRight] = true;
		//			availability[TeachingTipPlacementMode.LeftTop] = true;
		//			availability[TeachingTipPlacementMode.LeftBottom] = true;
		//			availability[TeachingTipPlacementMode.RightTop] = true;
		//			availability[TeachingTipPlacementMode.RightBottom] = true;
		//			availability[TeachingTipPlacementMode.Center] = true;

		//			double tipHeight = contentHeight + TailShortSideLength();
		//			double tipWidth = contentWidth + TailShortSideLength();

		//			// We try to avoid having the tail touch the HeroContent so rule out positions where this would be required
		//			if (HeroContent())
		//			{
		//				if (var heroContentBorder = m_heroContentBorder)
		//        {
		//					if (var nonHeroContentRootGrid = m_nonHeroContentRootGrid)
		//            {
		//						if (heroContentBorder.ActualHeight() > nonHeroContentRootGrid.ActualHeight() - TailLongSideActualLength())
		//						{
		//							availability[TeachingTipPlacementMode.Left] = false;
		//							availability[TeachingTipPlacementMode.Right] = false;
		//						}
		//					}
		//				}

		//				switch (HeroContentPlacement())
		//				{
		//					case TeachingTipHeroContentPlacementMode.Bottom:
		//						availability[TeachingTipPlacementMode.Top] = false;
		//						availability[TeachingTipPlacementMode.TopRight] = false;
		//						availability[TeachingTipPlacementMode.TopLeft] = false;
		//						availability[TeachingTipPlacementMode.RightTop] = false;
		//						availability[TeachingTipPlacementMode.LeftTop] = false;
		//						availability[TeachingTipPlacementMode.Center] = false;
		//						break;
		//					case TeachingTipHeroContentPlacementMode.Top:
		//						availability[TeachingTipPlacementMode.Bottom] = false;
		//						availability[TeachingTipPlacementMode.BottomLeft] = false;
		//						availability[TeachingTipPlacementMode.BottomRight] = false;
		//						availability[TeachingTipPlacementMode.RightBottom] = false;
		//						availability[TeachingTipPlacementMode.LeftBottom] = false;
		//						break;
		//				}
		//			}

		//			// When ShouldConstrainToRootBounds is true clippedTargetBounds == availableBoundsAroundTarget
		//			// We have to separate them because there are checks which care about both.
		//			var[clippedTargetBounds, availableBoundsAroundTarget] = DetermineSpaceAroundTarget();

		//			// If the edge of the target isn't in the window.
		//			if (clippedTargetBounds.Left < 0)
		//			{
		//				availability[TeachingTipPlacementMode.LeftBottom] = false;
		//				availability[TeachingTipPlacementMode.Left] = false;
		//				availability[TeachingTipPlacementMode.LeftTop] = false;
		//			}
		//			// If the right edge of the target isn't in the window.
		//			if (clippedTargetBounds.Right < 0)
		//			{
		//				availability[TeachingTipPlacementMode.RightBottom] = false;
		//				availability[TeachingTipPlacementMode.Right] = false;
		//				availability[TeachingTipPlacementMode.RightTop] = false;
		//			}
		//			// If the top edge of the target isn't in the window.
		//			if (clippedTargetBounds.Top < 0)
		//			{
		//				availability[TeachingTipPlacementMode.TopLeft] = false;
		//				availability[TeachingTipPlacementMode.Top] = false;
		//				availability[TeachingTipPlacementMode.TopRight] = false;
		//			}
		//			// If the bottom edge of the target isn't in the window
		//			if (clippedTargetBounds.Bottom < 0)
		//			{
		//				availability[TeachingTipPlacementMode.BottomLeft] = false;
		//				availability[TeachingTipPlacementMode.Bottom] = false;
		//				availability[TeachingTipPlacementMode.BottomRight] = false;
		//			}

		//			// If the horizontal midpoint is out of the window.
		//			if (clippedTargetBounds.Left < -m_currentTargetBoundsInCoreWindowSpace.Width / 2 ||
		//				clippedTargetBounds.Right < -m_currentTargetBoundsInCoreWindowSpace.Width / 2)
		//			{
		//				availability[TeachingTipPlacementMode.TopLeft] = false;
		//				availability[TeachingTipPlacementMode.Top] = false;
		//				availability[TeachingTipPlacementMode.TopRight] = false;
		//				availability[TeachingTipPlacementMode.BottomLeft] = false;
		//				availability[TeachingTipPlacementMode.Bottom] = false;
		//				availability[TeachingTipPlacementMode.BottomRight] = false;
		//				availability[TeachingTipPlacementMode.Center] = false;
		//			}

		//			// If the vertical midpoint is out of the window.
		//			if (clippedTargetBounds.Top < -m_currentTargetBoundsInCoreWindowSpace.Height / 2 ||
		//				clippedTargetBounds.Bottom < -m_currentTargetBoundsInCoreWindowSpace.Height / 2)
		//			{
		//				availability[TeachingTipPlacementMode.LeftBottom] = false;
		//				availability[TeachingTipPlacementMode.Left] = false;
		//				availability[TeachingTipPlacementMode.LeftTop] = false;
		//				availability[TeachingTipPlacementMode.RightBottom] = false;
		//				availability[TeachingTipPlacementMode.Right] = false;
		//				availability[TeachingTipPlacementMode.RightTop] = false;
		//				availability[TeachingTipPlacementMode.Center] = false;
		//			}

		//			// If the tip is too tall to fit between the top of the target and the top edge of the window or screen.
		//			if (tipHeight > availableBoundsAroundTarget.Top)
		//			{
		//				availability[TeachingTipPlacementMode.Top] = false;
		//				availability[TeachingTipPlacementMode.TopRight] = false;
		//				availability[TeachingTipPlacementMode.TopLeft] = false;
		//			}
		//			// If the total tip is too tall to fit between the center of the target and the top of the window.
		//			if (tipHeight > availableBoundsAroundTarget.Top + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		//			{
		//				availability[TeachingTipPlacementMode.Center] = false;
		//			}
		//			// If the tip is too tall to fit between the center of the target and the top edge of the window.
		//			if (contentHeight - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Top + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		//			{
		//				availability[TeachingTipPlacementMode.RightTop] = false;
		//				availability[TeachingTipPlacementMode.LeftTop] = false;
		//			}
		//			// If the tip is too tall to fit in the window when the tail is centered vertically on the target and the tip.
		//			if (contentHeight / 2.0f > availableBoundsAroundTarget.Top + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f) ||
		//				contentHeight / 2.0f > availableBoundsAroundTarget.Bottom + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		//			{
		//				availability[TeachingTipPlacementMode.Right] = false;
		//				availability[TeachingTipPlacementMode.Left] = false;
		//			}
		//			// If the tip is too tall to fit between the center of the target and the bottom edge of the window.
		//			if (contentHeight - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Bottom + (m_currentTargetBoundsInCoreWindowSpace.Height / 2.0f))
		//			{
		//				availability[TeachingTipPlacementMode.RightBottom] = false;
		//				availability[TeachingTipPlacementMode.LeftBottom] = false;
		//			}
		//			// If the tip is too tall to fit between the bottom of the target and the bottom edge of the window.
		//			if (tipHeight > availableBoundsAroundTarget.Bottom)
		//			{
		//				availability[TeachingTipPlacementMode.Bottom] = false;
		//				availability[TeachingTipPlacementMode.BottomLeft] = false;
		//				availability[TeachingTipPlacementMode.BottomRight] = false;
		//			}

		//			// If the tip is too wide to fit between the left edge of the target and the left edge of the window.
		//			if (tipWidth > availableBoundsAroundTarget.Left)
		//			{
		//				availability[TeachingTipPlacementMode.Left] = false;
		//				availability[TeachingTipPlacementMode.LeftTop] = false;
		//				availability[TeachingTipPlacementMode.LeftBottom] = false;
		//			}
		//			// If the tip is too wide to fit between the center of the target and the left edge of the window.
		//			if (contentWidth - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Left + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f))
		//			{
		//				availability[TeachingTipPlacementMode.TopLeft] = false;
		//				availability[TeachingTipPlacementMode.BottomLeft] = false;
		//			}
		//			// If the tip is too wide to fit in the window when the tail is centered horizontally on the target and the tip.
		//			if (contentWidth / 2.0f > availableBoundsAroundTarget.Left + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f) ||
		//				contentWidth / 2.0f > availableBoundsAroundTarget.Right + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f))
		//			{
		//				availability[TeachingTipPlacementMode.Top] = false;
		//				availability[TeachingTipPlacementMode.Bottom] = false;
		//				availability[TeachingTipPlacementMode.Center] = false;
		//			}
		//			// If the tip is too wide to fit between the center of the target and the right edge of the window.
		//			if (contentWidth - MinimumTipEdgeToTailCenter() > availableBoundsAroundTarget.Right + (m_currentTargetBoundsInCoreWindowSpace.Width / 2.0f))
		//			{
		//				availability[TeachingTipPlacementMode.TopRight] = false;
		//				availability[TeachingTipPlacementMode.BottomRight] = false;
		//			}
		//			// If the tip is too wide to fit between the right edge of the target and the right edge of the window.
		//			if (tipWidth > availableBoundsAroundTarget.Right)
		//			{
		//				availability[TeachingTipPlacementMode.Right] = false;
		//				availability[TeachingTipPlacementMode.RightTop] = false;
		//				availability[TeachingTipPlacementMode.RightBottom] = false;
		//			}

		//			var priorities = GetPlacementFallbackOrder(PreferredPlacement());

		//			foreach (var mode in priorities)
		//			{
		//				if (availability[mode])
		//				{
		//					return Tuple.Create(mode, false);
		//				}
		//			}
		//			// The teaching tip wont fit anywhere, set tipDoesNotFit to indicate that we should not open.
		//			return Tuple.Create(TeachingTipPlacementMode.Top, true);
		//		}

		//		Tuple<TeachingTipPlacementMode, bool> DetermineEffectivePlacementUntargeted(double contentHeight, double contentWidth)
		//		{
		//			var windowBounds = GetWindowBounds();
		//			if (!ShouldConstrainToRootBounds())
		//			{
		//				var screenBoundsInCoreWindowSpace = GetEffectiveScreenBoundsInCoreWindowSpace(windowBounds);
		//				if (screenBoundsInCoreWindowSpace.Height > contentHeight && screenBoundsInCoreWindowSpace.Width > contentWidth)
		//				{
		//					return Tuple.Create(TeachingTipPlacementMode.Bottom, false);
		//				}
		//			}
		//			else
		//			{
		//				var windowBoundsInCoreWindowSpace = GetEffectiveWindowBoundsInCoreWindowSpace(windowBounds);
		//				if (windowBoundsInCoreWindowSpace.Height > contentHeight && windowBoundsInCoreWindowSpace.Width > contentWidth)
		//				{
		//					return Tuple.Create(TeachingTipPlacementMode.Bottom, false);
		//				}
		//			}

		//			// The teaching tip doesn't fit in the window/screen set tipDoesNotFit to indicate that we should not open.
		//			return Tuple.Create(TeachingTipPlacementMode.Top, true);
		//		}

		//		Tuple<Thickness, Thickness> DetermineSpaceAroundTarget()
		//		{
		//			var shouldConstrainToRootBounds = ShouldConstrainToRootBounds();

		//			var[windowBoundsInCoreWindowSpace, screenBoundsInCoreWindowSpace] = [this]()

		//	{
		//				var windowBounds = GetWindowBounds();
		//				return Tuple.Create(GetEffectiveWindowBoundsInCoreWindowSpace(windowBounds),
		//									   GetEffectiveScreenBoundsInCoreWindowSpace(windowBounds));
		//			} ();


		//			Thickness windowSpaceAroundTarget{
		//				// Target.Left - Window.Left
		//				m_currentTargetBoundsInCoreWindowSpace.X - /* 0 except with test window bounds */ windowBoundsInCoreWindowSpace.X,
		//        // Target.Top - Window.Top
		//        m_currentTargetBoundsInCoreWindowSpace.Y - /* 0 except with test window bounds */ windowBoundsInCoreWindowSpace.Y,
		//        // Window.Right - Target.Right
		//        (windowBoundsInCoreWindowSpace.X + windowBoundsInCoreWindowSpace.Width) - (m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width),
		//        // Screen.Right - Target.Right
		//        (windowBoundsInCoreWindowSpace.Y + windowBoundsInCoreWindowSpace.Height) - (m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height) };


		//			Thickness screenSpaceAroundTarget = [this, screenBoundsInCoreWindowSpace, windowSpaceAroundTarget]()

		//	{
		//				if (!ShouldConstrainToRootBounds())
		//				{
		//					return Thickness{
		//						// Target.Left - Screen.Left
		//						m_currentTargetBoundsInCoreWindowSpace.X - screenBoundsInCoreWindowSpace.X,
		//                // Target.Top - Screen.Top
		//                m_currentTargetBoundsInCoreWindowSpace.Y - screenBoundsInCoreWindowSpace.Y,
		//                // Screen.Right - Target.Right
		//                (screenBoundsInCoreWindowSpace.X + screenBoundsInCoreWindowSpace.Width) - (m_currentTargetBoundsInCoreWindowSpace.X + m_currentTargetBoundsInCoreWindowSpace.Width),
		//                // Screen.Bottom - Target.Bottom
		//                (screenBoundsInCoreWindowSpace.Y + screenBoundsInCoreWindowSpace.Height) - (m_currentTargetBoundsInCoreWindowSpace.Y + m_currentTargetBoundsInCoreWindowSpace.Height) };
		//				}
		//				return windowSpaceAroundTarget;
		//			} ();

		//			return Tuple.Create(windowSpaceAroundTarget, screenSpaceAroundTarget);
		//		}

		//		Rect GetEffectiveWindowBoundsInCoreWindowSpace(Rect& windowBounds)
		//		{
		//			if (m_useTestWindowBounds)
		//			{
		//				return m_testWindowBoundsInCoreWindowSpace;
		//			}
		//			else
		//			{
		//				return Rect{ 0, 0, windowBounds.Width, windowBounds.Height };
		//			}

		//		}

		//		Rect GetEffectiveScreenBoundsInCoreWindowSpace(Rect& windowBounds)
		//		{
		//			if (!m_useTestScreenBounds && !ShouldConstrainToRootBounds())
		//			{
		//				MUX_ASSERT_MSG(!m_returnTopForOutOfWindowPlacement, "When returnTopForOutOfWindowPlacement is true we will never need to get the screen bounds");
		//				var displayInfo = DisplayInformation.GetForCurrentView();
		//				var scaleFactor = displayInfo.RawPixelsPerViewPixel();
		//				return Rect(-windowBounds.X,
		//					-windowBounds.Y,
		//					displayInfo.ScreenHeightInRawPixels() / (float)(scaleFactor),
		//					displayInfo.ScreenWidthInRawPixels() / (float)(scaleFactor));
		//			}
		//			return m_testScreenBoundsInCoreWindowSpace;
		//		}

		//		Rect GetWindowBounds()
		//		{
		//			if (UIElement10 uiElement10 = this)
		//    {
		//				if (var xamlRoot = uiElement10.XamlRoot())
		//        {
		//					return Rect{ 0, 0, xamlRoot.Size().Width, xamlRoot.Size().Height };
		//				}
		//			}
		//			return Window.Current().CoreWindow().Bounds();
		//		}

		//		std.array<TeachingTipPlacementMode, 13> GetPlacementFallbackOrder(TeachingTipPlacementMode preferredPlacement)
		//		{
		//			var priorityList = std.array < TeachingTipPlacementMode, 13 > ();
		//			priorityList[0] = TeachingTipPlacementMode.Top;
		//			priorityList[1] = TeachingTipPlacementMode.Bottom;
		//			priorityList[2] = TeachingTipPlacementMode.Left;
		//			priorityList[3] = TeachingTipPlacementMode.Right;
		//			priorityList[4] = TeachingTipPlacementMode.TopLeft;
		//			priorityList[5] = TeachingTipPlacementMode.TopRight;
		//			priorityList[6] = TeachingTipPlacementMode.BottomLeft;
		//			priorityList[7] = TeachingTipPlacementMode.BottomRight;
		//			priorityList[8] = TeachingTipPlacementMode.LeftTop;
		//			priorityList[9] = TeachingTipPlacementMode.LeftBottom;
		//			priorityList[10] = TeachingTipPlacementMode.RightTop;
		//			priorityList[11] = TeachingTipPlacementMode.RightBottom;
		//			priorityList[12] = TeachingTipPlacementMode.Center;


		//			if (IsPlacementBottom(preferredPlacement))
		//			{
		//				// Swap to bottom > top
		//				std.swap(priorityList[0], priorityList[1]);
		//				std.swap(priorityList[4], priorityList[6]);
		//				std.swap(priorityList[5], priorityList[7]);
		//			}
		//			else if (IsPlacementLeft(preferredPlacement))
		//			{
		//				// swap to lateral > vertical
		//				std.swap(priorityList[0], priorityList[2]);
		//				std.swap(priorityList[1], priorityList[3]);
		//				std.swap(priorityList[4], priorityList[8]);
		//				std.swap(priorityList[5], priorityList[9]);
		//				std.swap(priorityList[6], priorityList[10]);
		//				std.swap(priorityList[7], priorityList[11]);
		//			}
		//			else if (IsPlacementRight(preferredPlacement))
		//			{
		//				// swap to lateral > vertical
		//				std.swap(priorityList[0], priorityList[2]);
		//				std.swap(priorityList[1], priorityList[3]);
		//				std.swap(priorityList[4], priorityList[8]);
		//				std.swap(priorityList[5], priorityList[9]);
		//				std.swap(priorityList[6], priorityList[10]);
		//				std.swap(priorityList[7], priorityList[11]);

		//				// swap to right > left
		//				std.swap(priorityList[0], priorityList[1]);
		//				std.swap(priorityList[4], priorityList[6]);
		//				std.swap(priorityList[5], priorityList[7]);
		//			}

		//			//Switch the preferred placement to first.
		//			var pivot = std.find_if(priorityList.begin(),
		//				priorityList.end(),

		//				[preferredPlacement](TeachingTipPlacementMode mode). bool {
		//				return mode == preferredPlacement;
		//			});
		//			if (pivot != priorityList.end())
		//			{
		//				std.rotate(priorityList.begin(), pivot, pivot + 1);
		//			}

		//			return priorityList;
		//		}


		//		void EstablishShadows()
		//		{
		//# ifdef TAIL_SHADOW
		//# ifdef _DEBUG
		//			if (UIElement10 tailPolygon_uiElement10 = m_tailPolygon)
		//    {
		//				if (m_tipShadow)
		//				{
		//					if (!tailPolygon_uiElement10.Shadow())
		//					{
		//						// This facilitates an experiment around faking a proper tail shadow, shadows are expensive though so we don't want it present for release builds.
		//						var tailShadow = Windows.UI.Xaml.Media.ThemeShadow{ };
		//						tailShadow.Receivers().Append(m_target);
		//						tailPolygon_uiElement10.Shadow(tailShadow);
		//						if (var tailPolygon = m_tailPolygon)
		//                {
		//							var tailPolygonTranslation = tailPolygon.Translation()

		//					tailPolygon.Translation({ tailPolygonTranslation.x, tailPolygonTranslation.y, m_tailElevation });
		//						}
		//					}
		//				}
		//				else
		//				{
		//					tailPolygon_uiElement10.Shadow(null);
		//				}
		//			}
		//#endif
		//#endif
		//			if (UIElement10 m_contentRootGrid_uiElement10 = m_contentRootGrid)
		//    {
		//				if (m_tipShouldHaveShadow)
		//				{
		//					if (!m_contentRootGrid_uiElement10.Shadow())
		//					{
		//						m_contentRootGrid_uiElement10.Shadow(ThemeShadow{ });
		//						if (var contentRootGrid = m_contentRootGrid)
		//                {
		//							var contentRootGridTranslation = contentRootGrid.Translation();
		//							contentRootGrid.Translation({ contentRootGridTranslation.x, contentRootGridTranslation.y, m_contentElevation });
		//						}
		//					}
		//				}
		//				else
		//				{
		//					m_contentRootGrid_uiElement10.Shadow(null);
		//				}
		//			}
		//		}

		//		void TrySetCenterPoint(UIElement9& element, float3& centerPoint)
		//		{
		//			if (element)
		//			{
		//				element.CenterPoint(centerPoint);
		//			}
		//		}

		//		void OnPropertyChanged(
		//			 DependencyObject sender,
		//			 DependencyPropertyChangedEventArgs& args)
		//		{
		//			get_self<TeachingTip>(sender.as< TeachingTip > ()).OnPropertyChanged(args);
		//		}

		//		float TailLongSideActualLength()
		//		{
		//			if (var tailPolygon = m_tailPolygon)
		//    {
		//				return (float)(std.max(tailPolygon.ActualHeight(), tailPolygon.ActualWidth()));
		//			}
		//			return 0;
		//		}

		//		float TailLongSideLength()
		//		{
		//			return (float)(TailLongSideActualLength() - (2 * s_tailOcclusionAmount));
		//		}

		//		float TailShortSideLength()
		//		{
		//			if (var tailPolygon = m_tailPolygon)
		//    {
		//				return (float)(std.min(tailPolygon.ActualHeight(), tailPolygon.ActualWidth()) - s_tailOcclusionAmount);
		//			}
		//			return 0;
		//		}

		//		float MinimumTipEdgeToTailEdgeMargin()
		//		{
		//			if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//    {
		//				return tailOcclusionGrid.ColumnDefinitions().Size() > 1 ?
		//					(float)(tailOcclusionGrid.ColumnDefinitions().GetAt(1).ActualWidth() + s_tailOcclusionAmount)
		//					: 0.0f;
		//			}
		//			return 0;
		//		}

		//		float MinimumTipEdgeToTailCenter()
		//		{
		//			if (var tailOcclusionGrid = m_tailOcclusionGrid)
		//    {
		//				if (var tailPolygon = m_tailPolygon)
		//        {
		//					return tailOcclusionGrid.ColumnDefinitions().Size() > 1 ?
		//						(float)(tailOcclusionGrid.ColumnDefinitions().GetAt(0).ActualWidth() +
		//							tailOcclusionGrid.ColumnDefinitions().GetAt(1).ActualWidth() +
		//							(std.max(tailPolygon.ActualHeight(), tailPolygon.ActualWidth()) / 2))
		//						: 0.0f;
		//				}
		//			}
		//			return 0;
		//		}

		//		////////////////
		//		// Test Hooks //
		//		////////////////
		//		void SetExpandEasingFunction(CompositionEasingFunction& easingFunction)
		//		{
		//			m_expandEasingFunction = easingFunction;
		//			CreateExpandAnimation();
		//		}

		//		void SetContractEasingFunction(CompositionEasingFunction& easingFunction)
		//		{
		//			m_contractEasingFunction = easingFunction;
		//			CreateContractAnimation();
		//		}

		//		void SetTipShouldHaveShadow(bool tipShouldHaveShadow)
		//		{
		//			if (m_tipShouldHaveShadow != tipShouldHaveShadow)
		//			{
		//				m_tipShouldHaveShadow = tipShouldHaveShadow;
		//				EstablishShadows();
		//			}
		//		}

		//		void SetContentElevation(float elevation)
		//		{
		//			m_contentElevation = elevation;
		//			if (SharedHelpers.IsRS5OrHigher())
		//			{
		//				if (var contentRootGrid = m_contentRootGrid)
		//        {
		//					var contentRootGridTranslation = contentRootGrid.Translation();
		//					m_contentRootGrid.Translation({ contentRootGridTranslation.x, contentRootGridTranslation.y, m_contentElevation });
		//				}
		//				if (m_expandElevationAnimation)
		//				{
		//					m_expandElevationAnimation.SetScalarParameter("contentElevation", m_contentElevation);
		//				}
		//			}
		//		}

		//		void SetTailElevation(float elevation)
		//		{
		//			m_tailElevation = elevation;
		//			if (SharedHelpers.IsRS5OrHigher() && m_tailPolygon)
		//			{
		//				if (var tailPolygon = m_tailPolygon)
		//        {
		//					var tailPolygonTranslation = tailPolygon.Translation();
		//					tailPolygon.Translation({ tailPolygonTranslation.x, tailPolygonTranslation.y, m_tailElevation });
		//				}
		//			}
		//		}

		//		void SetUseTestWindowBounds(bool useTestWindowBounds)
		//		{
		//			m_useTestWindowBounds = useTestWindowBounds;
		//		}

		//		void SetTestWindowBounds(Rect& testWindowBounds)
		//		{
		//			m_testWindowBoundsInCoreWindowSpace = testWindowBounds;
		//		}

		//		void SetUseTestScreenBounds(bool useTestScreenBounds)
		//		{
		//			m_useTestScreenBounds = useTestScreenBounds;
		//		}

		//		void SetTestScreenBounds(Rect& testScreenBounds)
		//		{
		//			m_testScreenBoundsInCoreWindowSpace = testScreenBounds;
		//		}

		//		void SetTipFollowsTarget(bool tipFollowsTarget)
		//		{
		//			if (m_tipFollowsTarget != tipFollowsTarget)
		//			{
		//				m_tipFollowsTarget = tipFollowsTarget;
		//				if (tipFollowsTarget)
		//				{
		//					if (var target = m_target)
		//            {
		//						SetViewportChangedEvent(gsl.make_strict_not_null(target));
		//					}
		//				}
		//				else
		//				{
		//					RevokeViewportChangedEvent();
		//				}
		//			}
		//		}

		//		void SetReturnTopForOutOfWindowPlacement(bool returnTopForOutOfWindowPlacement)
		//		{
		//			m_returnTopForOutOfWindowPlacement = returnTopForOutOfWindowPlacement;
		//		}

		//		void SetExpandAnimationDuration(TimeSpan& expandAnimationDuration)
		//		{
		//			m_expandAnimationDuration = expandAnimationDuration;
		//			if (var expandAnimation = m_expandAnimation)
		//    {
		//				expandAnimation.Duration(m_expandAnimationDuration);
		//			}
		//			if (var expandElevationAnimation = m_expandElevationAnimation)
		//    {
		//				expandElevationAnimation.Duration(m_expandAnimationDuration);
		//			}
		//		}

		//		void SetContractAnimationDuration(TimeSpan& contractAnimationDuration)
		//		{
		//			m_contractAnimationDuration = contractAnimationDuration;
		//			if (var contractAnimation = m_contractAnimation)
		//    {
		//				contractAnimation.Duration(m_contractAnimationDuration);
		//			}
		//			if (var contractElevationAnimation = m_contractElevationAnimation)
		//    {
		//				contractElevationAnimation.Duration(m_contractAnimationDuration);
		//			}
		//		}

		//		bool GetIsIdle()
		//		{
		//			return m_isIdle;
		//		}

		//		void SetIsIdle(bool isIdle)
		//		{
		//			if (m_isIdle != isIdle)
		//			{
		//				m_isIdle = isIdle;
		//				TeachingTipTestHooks.NotifyIdleStatusChanged(this);
		//			}
		//		}

		//		TeachingTipPlacementMode GetEffectivePlacement()
		//		{
		//			return m_currentEffectiveTipPlacementMode;
		//		}

		//		TeachingTipHeroContentPlacementMode GetEffectiveHeroContentPlacement()
		//		{
		//			return m_currentHeroContentEffectivePlacementMode;
		//		}

		//		double GetHorizontalOf =
		//		{
		//    if (var popup = m_popup)

		//	{
		//        return popup.HorizontalOf = ;
		//	}
		//    return 0.0;
		//}

		//double GetVerticalOf =
		//{
		//    if (var popup = m_popup)
		//    {
		//	return popup.VerticalOf = ;
		//}
		//return 0.0;
		//}

		private Visibility GetTitleVisibility()
		{
			var titleTextBox = m_titleTextBox;
			if (titleTextBox != null)
		    {
				return titleTextBox.Visibility;
			}
			return Visibility.Collapsed;
		}

		private Visibility GetSubtitleVisibility()
		{
			var subtitleTextBox = m_subtitleTextBox;
			if (subtitleTextBox != null)
		    {
				return subtitleTextBox.Visibility;
			}
			return Visibility.Collapsed;
		}

		private void UpdatePopupRequestedTheme()
		{
			// The way that TeachingTip reparents its content tree breaks ElementTheme calculations. Hook up a listener to
			// ActualTheme on the TeachingTip and then set the Popup's RequestedTheme to match when it changes.
			FrameworkElement frameworkElement6 = this;
			if (frameworkElement6 != null)
			{
				// Uno specific - instead of a revoker, use boolean flag to check if event handler was attached
				if (!m_actualThemeChangedAttached)
				{
					frameworkElement6.ActualThemeChanged += OnActualThemeChanged;
					m_actualThemeChangedAttached = true;
				}

				var popup = m_popup;
				if (popup != null)
		        {
					popup.RequestedTheme = frameworkElement6.ActualTheme;
				}
			}
		}

		// Uno specific - use a method instead of lambda to allow unhooking the event

		private bool m_actualThemeChangedAttached = false;

		private void OnActualThemeChanged(object sender, object args)
		{
			UpdatePopupRequestedTheme();
		}
	}
}
