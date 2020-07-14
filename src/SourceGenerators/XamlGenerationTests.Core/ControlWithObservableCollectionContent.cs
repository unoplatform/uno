using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared.Controls
{
	[ContentProperty(Name = "Items")]
	public partial class ControlWithObservableCollectionContent : Control
	{
		public ObservableCollection<object> Items
		{
			get { return (ObservableCollection<object>)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(ObservableCollection<object>), typeof(ControlWithObservableCollectionContent), new FrameworkPropertyMetadata(new ObservableCollection<object>()));


	}
}
