// MUX reference PagerControlTemplateSettings.properties.cpp, commit a08f765

using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PagerControlTemplateSettings : DependencyObject
	{
		public PagerControlTemplateSettings()
		{
		}

		public IList<object> Pages
		{
			get => (IList<object>)GetValue(PagesProperty);
			set => SetValue(PagesProperty, value);
		}

		public static DependencyProperty PagesProperty { get; } =
			DependencyProperty.Register(nameof(Pages), typeof(IList<object>), typeof(PagerControlTemplateSettings), new FrameworkPropertyMetadata(null));

		public IList<object> NumberPanelItems
		{
			get { return (IList<object>)GetValue(NumberPanelItemsProperty); }
			set { SetValue(NumberPanelItemsProperty, value); }
		}

		public static DependencyProperty NumberPanelItemsProperty { get; } =
			DependencyProperty.Register(nameof(NumberPanelItems), typeof(IList<object>), typeof(PagerControlTemplateSettings), new FrameworkPropertyMetadata(null));
	}
}
