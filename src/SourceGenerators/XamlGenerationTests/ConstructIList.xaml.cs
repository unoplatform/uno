using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{
	public sealed partial class ConstructIList : UserControl
	{
		public ConstructIList()
		{
			this.InitializeComponent();
		}
	}

	[ContentProperty(Name = nameof(TabItems))]
	public partial class TestTabControl : Control
	{
		public IList<object> TabItems
		{
			get => (IList<object>)GetValue(TabItemsProperty);
			set => SetValue(TabItemsProperty, value);
		}

		public static DependencyProperty TabItemsProperty { get; } =
			DependencyProperty.Register(nameof(TabItems), typeof(IList<object>), typeof(TestTabControl), new PropertyMetadata(null));
	}
}
