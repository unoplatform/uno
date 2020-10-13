// MUX Reference: TabViewItem.cpp, commit 542e6f9

using System.Numerics;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabViewItem : ListViewItem
	{
		private const string c_overlayCornerRadiusKey = "OverlayCornerRadius";
		private const string SR_TabViewCloseButtonName = "TabViewCloseButtonName";

		private bool m_firstTimeSettingToolTip = true;
		private bool m_hasPointerCapture = false;
		private bool m_isMiddlePointerButtonPressed = false;
		private bool m_isDragging = false;
		private bool m_isPointerOver = false;
		private TabViewCloseButtonOverlayMode m_closeButtonOverlayMode = TabViewCloseButtonOverlayMode.Auto;
		private TabViewWidthMode m_tabViewWidthMode = TabViewWidthMode.Equal;
		private Button m_closeButton;
		private ToolTip m_toolTip;
		private object m_shadow;
		private TabView m_parentTabView;

		public TabViewItem()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_TabViewItem);

			DefaultStyleKey = typeof(TabViewItem);

			SetValue(TabViewTemplateSettingsProperty, new TabViewItemTemplateSettings());

			RegisterPropertyChangedCallback(SelectorItem.IsSelectedProperty, OnIsSelectedPropertyChanged);
		}

		protected override void OnApplyTemplate()
		{
			var popupRadius = (CornerRadius)ResourceAccessor.ResourceLookup(this, c_overlayCornerRadiusKey);

			var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
			var internalTabView = tabView ?? null;

			Button GetCloseButton(TabView internalTabView)
			{
				var closeButton = (Button)GetTemplateChild("CloseButton");
				if (closeButton != null)
				{
					// Do localization for the close button automation name
					if (string.IsNullOrEmpty(AutomationProperties.GetName(closeButton)))
					{
						var closeButtonName = ResourceAccessor.GetLocalizedStringResource(SR_TabViewCloseButtonName);
						AutomationProperties.SetName(closeButton, closeButtonName);
					}

					if (internalTabView != null)
					{
						// Setup the tooltip for the close button
						var tooltip = new ToolTip();
						tooltip.Content = internalTabView.GetTabCloseButtonTooltipText();
						ToolTipService.SetToolTip(closeButton, tooltip);
					}

					closeButton.Click += OnCloseButtonClick;
				}
				return closeButton;
			}
			m_closeButton = GetCloseButton(internalTabView);

			OnIconSourceChanged();

			if (tabView != null)
			{
				if (SharedHelpers.IsThemeShadowAvailable())
				{
					if (internalTabView != null)
					{
						var shadow = new ThemeShadow();
						shadow.Receivers.Add(internalTabView.GetShadowReceiver());
						m_shadow = shadow;

						double shadowDepth = (double)SharedHelpers.FindInApplicationResources(TabView.c_tabViewShadowDepthName, TabView.c_tabShadowDepth);

						var currentTranslation = Translation;
						var translation = new Vector3(currentTranslation.X, currentTranslation.Y, (float)shadowDepth);
						Translation = translation;

						UpdateShadow();
					}
				}

				tabView.TabDragStarting += OnTabDragStarting;
				tabView.TabDragCompleted += OnTabDragCompleted;
			}

			UpdateCloseButton();
			UpdateWidthModeVisualState();
		}

		private void OnIsSelectedPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
			if (peer != null)
			{
				peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
			}

			if (IsSelected)
			{
				SetValue(Canvas.ZIndexProperty, 20);
			}
			else
			{
				SetValue(Canvas.ZIndexProperty, 0);
			}

			UpdateShadow();
			UpdateWidthModeVisualState();

			UpdateCloseButton();
		}

		private void UpdateShadow()
		{
			if (SharedHelpers.IsThemeShadowAvailable())
			{
				if (IsSelected && !m_isDragging)
				{
					Shadow = (ThemeShadow)m_shadow;
				}
				else
				{
					Shadow = null;
				}
			}
		}

		private void OnTabDragStarting(object sender, TabViewTabDragStartingEventArgs args)
		{
			m_isDragging = true;
			UpdateShadow();
		}

		private void OnTabDragCompleted(object sender, TabViewTabDragCompletedEventArgs args)
		{
			m_isDragging = false;
			UpdateShadow();
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TabViewItemAutomationPeer(this);
		}

		internal void OnCloseButtonOverlayModeChanged(TabViewCloseButtonOverlayMode mode)
		{
			m_closeButtonOverlayMode = mode;
			UpdateCloseButton();
		}

		internal TabView GetParentTabView()
		{
			return m_parentTabView;
		}

		internal void SetParentTabView(TabView tabView)
		{
			m_parentTabView = tabView;
		}

		internal void OnTabViewWidthModeChanged(TabViewWidthMode mode)
		{
			m_tabViewWidthMode = mode;
			UpdateWidthModeVisualState();
		}


		private void UpdateCloseButton()
		{
			if (!IsClosable)
			{
				VisualStateManager.GoToState(this, "CloseButtonCollapsed", false);
			}
			else
			{
				switch (m_closeButtonOverlayMode)
				{
					case TabViewCloseButtonOverlayMode.OnPointerOver:
						{
							// If we only want to show the button on hover, we also show it when we are selected, otherwise hide it
							if (IsSelected || m_isPointerOver)
							{
								VisualStateManager.GoToState(this, "CloseButtonVisible", false);
							}
							else
							{
								VisualStateManager.GoToState(this, "CloseButtonCollapsed", false);
							}
							break;
						}
					default:
						{
							// Default, use "Auto"
							VisualStateManager.GoToState(this, "CloseButtonVisible", false);
							break;
						}
				}
			}
		}

		private void UpdateWidthModeVisualState()
		{
			// Handling compact/non compact width mode
			if (!IsSelected && m_tabViewWidthMode == TabViewWidthMode.Compact)
			{
				VisualStateManager.GoToState(this, "Compact", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "StandardWidth", false);
			}
		}

		private void RequestClose()
		{
			var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
			if (tabView != null)
			{
				var internalTabView = tabView;
				if (internalTabView != null)
				{
					internalTabView.RequestCloseTab(this);
				}
			}
		}

		internal void RaiseRequestClose(TabViewTabCloseRequestedEventArgs args)
		{
			// This should only be called from TabView, to ensure that both this event and the TabView TabRequestedClose event are raised
			CloseRequested?.Invoke(this, args);
		}

		private void OnCloseButtonClick(object sender, RoutedEventArgs args)
		{
			RequestClose();
		}

		private void OnIsClosablePropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateCloseButton();
		}

		private void OnHeaderPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (m_firstTimeSettingToolTip)
			{
				m_firstTimeSettingToolTip = false;

				if (ToolTipService.GetToolTip(this) == null)
				{
					// App author has not specified a tooltip; use our own
					ToolTip CreateToolTip()
					{
						var toolTip = new ToolTip();
						toolTip.Placement = PlacementMode.Mouse;
						ToolTipService.SetToolTip(this, toolTip);
						return toolTip;
					}

					m_toolTip = CreateToolTip();
				}
			}

			var toolTip = m_toolTip;
			if (toolTip != null)
			{
				// Update tooltip text to new header text
				var headerContent = Header;
				var potentialString = headerContent as IPropertyValue;

				if (potentialString != null && potentialString.Type == PropertyType.String)
				{
					toolTip.Content = headerContent;
				}
				else
				{
					toolTip.Content = null;
				}
			}
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			if (IsSelected && args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
			{
				var pointerPoint = args.GetCurrentPoint(this);
				if (pointerPoint.Properties.IsLeftButtonPressed)
				{
					var isCtrlDown = (Windows.UI.Xaml.Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
					if (isCtrlDown)
					{
						// Return here so the base class will not pick it up, but let it remain unhandled so someone else could handle it.
						return;
					}
				}
			}

			base.OnPointerPressed(args);

			if (args.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed)
			{
				if (CapturePointer(args.Pointer))
				{
					m_hasPointerCapture = true;
					m_isMiddlePointerButtonPressed = true;
				}
			}
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			if (m_hasPointerCapture)
			{
				if (args.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
				{
					bool wasPressed = m_isMiddlePointerButtonPressed;
					m_isMiddlePointerButtonPressed = false;
					ReleasePointerCapture(args.Pointer);

					if (wasPressed)
					{
						if (IsClosable)
						{
							RequestClose();
						}
					}
				}
			}
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);

			m_isPointerOver = true;

			if (m_hasPointerCapture)
			{
				m_isMiddlePointerButtonPressed = true;
			}

			UpdateCloseButton();
		}

		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
			base.OnPointerExited(args);

			m_isPointerOver = false;
			m_isMiddlePointerButtonPressed = false;

			UpdateCloseButton();
		}

		protected override void OnPointerCanceled(PointerRoutedEventArgs args)
		{
			base.OnPointerCanceled(args);

			if (m_hasPointerCapture)
			{
				ReleasePointerCapture(args.Pointer);
				m_isMiddlePointerButtonPressed = false;
			}
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		{
			base.OnPointerCaptureLost(args);

			m_hasPointerCapture = false;
			m_isMiddlePointerButtonPressed = false;
		}

		private void OnIconSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			OnIconSourceChanged();
		}

		private void OnIconSourceChanged()
		{
			var templateSettings = TabViewTemplateSettings;
			var source = this.IconSource;
			if (source != null)
			{
				templateSettings.IconElement = SharedHelpers.MakeIconElementFrom(source);
				VisualStateManager.GoToState(this, "Icon", false);
			}

			else
			{
				templateSettings.IconElement = null;
				VisualStateManager.GoToState(this, "NoIcon", false);
			}
		}
	}
}
