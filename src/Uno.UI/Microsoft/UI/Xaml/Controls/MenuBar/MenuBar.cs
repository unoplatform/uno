// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MenuBar.cpp, tag winui3/release/1.4.2

using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Uno;
using Uno.Disposables;
using MenuBarAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.MenuBarAutomationPeer;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class MenuBar : Control
	{
		private SerialDisposable m_itemsVectorChangedRevoker = new SerialDisposable();
		private Grid m_layoutRoot;
		private ItemsControl m_contentRoot;

		public MenuBar()
		{
			DefaultStyleKey = typeof(MenuBar);

			var items = new ObservableVector<MenuBarItem>();
			SetValue(ItemsProperty, items);

			var observableVector = Items as IObservableVector<MenuBarItem>;
			VectorChangedEventHandler<MenuBarItem> vectorChangedEventHandler = (_, _) => { UpdateAutomationSizeAndPosition(); };
			observableVector.VectorChanged += vectorChangedEventHandler;
			m_itemsVectorChangedRevoker.Disposable = new DisposableAction(() => observableVector.VectorChanged -= vectorChangedEventHandler);
		}

		// IUIElement / IUIElementOverridesHelper
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new MenuBarAutomationPeer(this);
		}

		protected override void OnApplyTemplate()
		{
			SetUpTemplateParts();
		}

		private void SetUpTemplateParts()
		{
			if (GetTemplateChild<Grid>("LayoutRoot") is { } layoutRoot)
			{
				m_layoutRoot = layoutRoot;
			}
			if (GetTemplateChild<ItemsControl>("ContentRoot") is { } contentRoot)
			{
				contentRoot.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

				var observableVector = Items as IObservableVector<MenuBarItem>;
				contentRoot.ItemsSource = observableVector;

				m_contentRoot = contentRoot;
			}
		}

		internal void RequestPassThroughElement(MenuBarItem menuBarItem)
		{
			// To enable switching flyout on hover, every menubar item needs the MenuBar root to include it for hit detection with flyouts open
			menuBarItem.AddPassThroughElement(m_layoutRoot);
		}

		internal bool IsFlyoutOpen { get; set; }

		// Automation Properties
		private void UpdateAutomationSizeAndPosition()
		{
			var sizeOfSet = 0;

			foreach (var item in Items)
			{
				if (item is UIElement itemAsUIElement)
				{
					if (itemAsUIElement.Visibility == Visibility.Visible)
					{
						sizeOfSet++;
						AutomationProperties.SetPositionInSet(itemAsUIElement, sizeOfSet);
					}
				}
			}

			foreach (var item in Items)
			{
				if (item is UIElement itemAsUIElement)
				{
					if (itemAsUIElement.Visibility == Visibility.Visible)
					{
						AutomationProperties.SetSizeOfSet(itemAsUIElement, sizeOfSet);
					}
				}
			}
		}
	}
}
