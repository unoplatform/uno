using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		private readonly ObservableVector<object> _menuItems;
		private NavigationViewList _menuItemsHost;

		public NavigationView()
		{
			_menuItems = new ObservableVector<object>();
			SetValue(MenuItemsProperty, _menuItems);
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
				item.PointerPressed += OnSettingsPressed;
			}

			OnIsSettingsVisibleChanged();
		}

		private void OnSettingsPressed(object sender, Input.PointerRoutedEventArgs e)
		{
			_menuItemsHost.SelectedItem = null;

			if (SettingsItem is NavigationViewItem item)
			{
				item.IsSelected = true;
				SelectionChanged?.Invoke(this, new NavigationViewSelectionChangedEventArgs { IsSettingsSelected = true, SelectedItem = SettingsItem });
			}
		}

		private void OnMenuItemsHost_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItem = _menuItemsHost.SelectedItem;

			SelectionChanged?.Invoke(this, new NavigationViewSelectionChangedEventArgs { IsSettingsSelected = false, SelectedItem = SelectedItem });

			foreach(var item in MenuItems.OfType<NavigationViewItem>())
			{
				if(item.SelectionIndicator != null)
				{
					item.SelectionIndicator.Opacity = SelectedItem == item ? 1 : 0;
				}
			}

			if (SettingsItem is NavigationViewItem settings)
			{
				settings.IsSelected = false;
			}
		}

		private void OnIsSettingsVisibleChanged()
		{
			VisualStateManager.GoToState(this, IsSettingsVisible ? "SettingsVisible" : "SettingsCollapsed", true);
		}
	}
}
