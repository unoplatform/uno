using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
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
			VisualStateManager.GoToState(this, IsBackButtonVisible != NavigationViewBackButtonVisible.Collapsed ? "MinimalWithBackButton" : "Minimal", true);

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
			VisualStateManager.GoToState(this, "Expanded", true);

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
			VisualStateManager.GoToState(this, "Compact", true);

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
				_menuItemsHost.SelectionChanged += OnMenuItemsHost_SelectionChanged;

				if(MenuItemsSource == null)
				{
					_menuItemsHost.Items.Clear();
					_menuItemsHost.Items.AddRange(_menuItems);
				}
			}

			if(SettingsItem is NavigationViewItem item)
			{
				item.Content = "Settings";
				item.InternalPointerPressed += OnSettingsPressed;
			}

			OnIsSettingsVisibleChanged();
		}

		private void OnSettingsPressed()
		{
			_menuItemsHost.SelectedItem = null;

			if (SettingsItem is NavigationViewItem item)
			{
				SelectedItem = SettingsItem;
				item.IsSelected = true;
				UpdateSelectedItem();
			}
		}

		private void OnMenuItemsHost_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItem = _menuItemsHost.SelectedItem;

			UpdateSelectedItem();
		}

		private void UpdateSelectedItem()
		{
			void updateOpacity(NavigationViewItem item) {
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
	}
}
