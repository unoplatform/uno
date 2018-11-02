using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		private readonly SerialDisposable _subscriptions = new SerialDisposable();
		private readonly ObservableVector<object> _menuItems;
		private NavigationViewList _menuItemsHost;
		private Grid _paneContentGrid;
		private Grid _buttonHolderGrid;
		private TextBlock _paneTitleTextBlock;
		private Button _togglePaneButton;
		private Button _navigationViewBackButton;

		public NavigationView()
		{
			_menuItems = new ObservableVector<object>();
			SetValue(MenuItemsProperty, _menuItems);
			SizeChanged += NavigationView_SizeChanged;
			Loaded += (s, e) => RegisterEvents();
			Unloaded += (s, e) => UnregisterEvents();
		}

		private void NavigationView_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			UpdatePositions();
		}

		private void UpdatePositions()
		{
			if (RenderSize.Width < CompactModeThresholdWidth)
			{
				UpdateCompactMode();
			}
			else if (RenderSize.Width > ExpandedModeThresholdWidth)
			{
				UpdateExpandedMode();
			}
			else
			{
				MinimalMode();
			}
		}

		private void MinimalMode()
		{
			DisplayMode = NavigationViewDisplayMode.Minimal;

			if (_paneContentGrid != null && _buttonHolderGrid != null)
			{
				_paneContentGrid.RowDefinitions.ElementAt(1).Height = GridLengthHelper.FromPixels(0);
			}

			if (_togglePaneButton != null && _navigationViewBackButton != null)
			{
				_togglePaneButton.Margin = new Thickness(0, _navigationViewBackButton.RenderSize.Height, 0, 0);
			}

			if (_paneTitleTextBlock != null)
			{
				_paneTitleTextBlock.Margin = new Thickness(0, 0, 0, 0);
			}
		}

		private void UpdateExpandedMode()
		{
			DisplayMode = NavigationViewDisplayMode.Expanded;

			if (_paneContentGrid != null && _navigationViewBackButton != null)
			{
				_paneContentGrid.RowDefinitions.ElementAt(1).Height = GridLengthHelper.FromPixels(_navigationViewBackButton.RenderSize.Height);
			}

			if (_paneTitleTextBlock != null && _togglePaneButton != null)
			{
				_paneTitleTextBlock.Margin = new Thickness(_togglePaneButton.RenderSize.Width, 0, 0, 0);
			}

			if (_togglePaneButton != null && _navigationViewBackButton != null)
			{
				_togglePaneButton.Margin = new Thickness(0, _navigationViewBackButton.RenderSize.Height, 0, 0);
			}
		}

		private void UpdateCompactMode()
		{
			DisplayMode = NavigationViewDisplayMode.Compact;

			if (_paneContentGrid != null && _buttonHolderGrid != null)
			{
				_paneContentGrid.RowDefinitions.ElementAt(1).Height = GridLengthHelper.FromPixels(0);
			}

			if (_paneTitleTextBlock != null)
			{
				_paneTitleTextBlock.Margin = new Thickness(0, 0, 0, 0);
			}

			if (_togglePaneButton != null && _navigationViewBackButton != null)
			{
				_togglePaneButton.Margin = new Thickness(_navigationViewBackButton.RenderSize.Width, 0, 0, 0);
			}
		}

		[Uno.NotImplemented]
		public object MenuItemFromContainer(DependencyObject container)
		{
			throw new global::System.NotImplementedException("The member object NavigationView.MenuItemFromContainer(DependencyObject container) is not implemented in Uno.");
		}

		[Uno.NotImplemented]
		public DependencyObject ContainerFromMenuItem(object item)
		{
			throw new global::System.NotImplementedException("The member DependencyObject NavigationView.ContainerFromMenuItem(object item) is not implemented in Uno.");
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_menuItemsHost = GetTemplateChild("MenuItemsHost") as NavigationViewList;
			_paneContentGrid = GetTemplateChild("PaneContentGrid") as Grid;
			_buttonHolderGrid = GetTemplateChild("ButtonHolderGrid") as Grid;
			_paneTitleTextBlock = GetTemplateChild("PaneTitleTextBlock") as TextBlock;
			_togglePaneButton = GetTemplateChild("TogglePaneButton") as Button;
			_navigationViewBackButton = GetTemplateChild("NavigationViewBackButton") as Button;

			SetValue(SettingsItemProperty, GetTemplateChild("SettingsNavPaneItem"));

			if(_menuItemsHost != null)
			{
				if (MenuItemsSource == null)
				{
					_menuItemsHost.Items.Clear();
					_menuItemsHost.Items.AddRange(_menuItems);
				}
			}

			if (SettingsItem is NavigationViewItem item)
			{
				item.Content = "Settings";
			}

			OnIsSettingsVisibleChanged();
			RegisterEvents();
		}

		private void RegisterEvents()
		{
			_subscriptions.Disposable = null;

			if (IsLoaded)
			{
				if (_menuItemsHost != null)
				{
					_menuItemsHost.SelectionChanged += OnMenuItemsHost_SelectionChanged;
					_menuItemsHost.ItemClick += OnMenuItemsHost_ItemClick;
				}

				if (_togglePaneButton != null)
				{
					_togglePaneButton.Click += OnTogglePaneButton_Click;
				}

				if (_navigationViewBackButton != null)
				{
					_navigationViewBackButton.Click += OnNavigationViewBackButtonClick;
				}

				if (SettingsItem is NavigationViewItem item)
				{
					item.InternalPointerPressed += OnSettingsPressed;
				}

				_subscriptions.Disposable = Disposable.Create(() => {
					if (_menuItemsHost != null)
					{
						_menuItemsHost.SelectionChanged -= OnMenuItemsHost_SelectionChanged;
						_menuItemsHost.ItemClick -= OnMenuItemsHost_ItemClick;
					}

					if (_togglePaneButton != null)
					{
						_togglePaneButton.Click -= OnTogglePaneButton_Click;
					}

					if (_navigationViewBackButton != null)
					{
						_navigationViewBackButton.Click -= OnNavigationViewBackButtonClick;
					}

					if (SettingsItem is NavigationViewItem item2)
					{
						item2.InternalPointerPressed -= OnSettingsPressed;
					}
				});
			}
		}

		private void UnregisterEvents()
		{
			_subscriptions.Disposable = null;
		}

		private void OnMenuItemsHost_ItemClick(object sender, ItemClickEventArgs e)
		{
			ItemInvoked?.Invoke(
				this,
				new NavigationViewItemInvokedEventArgs {
					InvokedItem = e.ClickedItem,
					IsSettingsInvoked = SettingsItem == e.ClickedItem
				}
			);
		}

		private void OnNavigationViewBackButtonClick(object sender, RoutedEventArgs e)
		{
			BackRequested?.Invoke(this, new NavigationViewBackRequestedEventArgs());
		}

		private void OnTogglePaneButton_Click(object sender, RoutedEventArgs e)
		{
			var closing = new NavigationViewPaneClosingEventArgs();

			if (IsPaneOpen)
			{
				PaneClosing?.Invoke(this, closing);

				if (closing.Cancel)
				{
					this.Log().DebugIfEnabled(() => "Close pane canceled");
					return;
				}
			}
			else
			{
				PaneOpening?.Invoke(this, null);
			}

			IsPaneOpen = !IsPaneOpen;

			if (IsPaneOpen)
			{
				PaneOpened?.Invoke(this, null);
			}
			else
			{
				PaneClosed?.Invoke(this, null);
			}
		}

		private void OnIsPaneOpenChanged()
		{
			foreach (var item in MenuItems.OfType<NavigationViewItemHeader>())
			{
				VisualStateManager.GoToState(item, IsPaneOpen ? "HeaderTextVisible" : "HeaderTextCollapsed", true);
			}

			VisualStateManager.GoToState(this, IsPaneOpen ? "AutoSuggestBoxVisible" : "AutoSuggestBoxCollapsed", true);
		}

		private void OnSettingsPressed()
		{
			if (SettingsItem is NavigationViewItem item)
			{
				SelectedItem = SettingsItem;
				_menuItemsHost.SelectedItem = null;

				ItemInvoked?.Invoke(
					this,
					new NavigationViewItemInvokedEventArgs
					{
						InvokedItem = SettingsItem,
						IsSettingsInvoked = true
					}
				);
			}
		}

		private void OnMenuItemsHost_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_menuItemsHost.SelectedItem == null && SelectedItem == SettingsItem)
			{
				// Ignore the change, we just selected the settings.
			}
			else
			{
				SelectedItem = _menuItemsHost.SelectedItem;
			}
		}

		private void OnSelectedItemChanged()
		{
			void updateOpacity(NavigationViewItem item)
			{
				if (item.SelectionIndicator != null)
				{
					item.SelectionIndicator.Opacity = SelectedItem == item ? 1 : 0;
				}
			}

			SelectionChanged?.Invoke(
				this,
				new NavigationViewSelectionChangedEventArgs {
					IsSettingsSelected = SelectedItem == SettingsItem, SelectedItem = SelectedItem
				}
			);

			foreach (var item in MenuItems.OfType<NavigationViewItem>())
			{
				updateOpacity(item);
			}

			if (SettingsItem is NavigationViewItem settings)
			{
				updateOpacity(settings);
				settings.IsSelected = SelectedItem == settings;
			}
		}

		private void OnIsSettingsVisibleChanged()
		{
			VisualStateManager.GoToState(this, IsSettingsVisible ? "SettingsVisible" : "SettingsCollapsed", true);
		}

		private void OnPaneToggleButtonVisibleChanged()
		{
			VisualStateManager.GoToState(this, IsPaneToggleButtonVisible ? "TogglePaneButtonVisible" : "TogglePaneButtonCollapsed", true);
		}

		private void OnDisplayModeChanged()
		{
			switch (DisplayMode)
			{
				case NavigationViewDisplayMode.Expanded:
					VisualStateManager.GoToState(this, "Expanded", true);
					break;

				case NavigationViewDisplayMode.Compact:
					VisualStateManager.GoToState(this, "Compact", true);
					break;

				case NavigationViewDisplayMode.Minimal:
					VisualStateManager.GoToState(this, IsBackButtonVisible != NavigationViewBackButtonVisible.Collapsed ? "MinimalWithBackButton" : "Minimal", true);
					break;
			}

			DisplayModeChanged?.Invoke(this, new NavigationViewDisplayModeChangedEventArgs { DisplayMode = DisplayMode });
		}
	}
}
