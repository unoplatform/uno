using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class MenuBarItem : Control
	{
		public string Title
		{
			get => (string)this.GetValue(TitleProperty);
			set => this.SetValue(TitleProperty, value);
		}

		public IList<MenuFlyoutItemBase> Items => (IList<MenuFlyoutItemBase>)this.GetValue(ItemsProperty);

		public static DependencyProperty ItemsProperty { get; } =
			DependencyProperty.Register(
				"Items",
				typeof(IList<MenuFlyoutItemBase>),
				typeof(MenuBarItem),
				new FrameworkPropertyMetadata(default(IList<MenuFlyoutItemBase>)));

		public static DependencyProperty TitleProperty { get; } =
			DependencyProperty.Register(
				"Title",
				typeof(string),
				typeof(MenuBarItem),
				new FrameworkPropertyMetadata(string.Empty));
	}
}
