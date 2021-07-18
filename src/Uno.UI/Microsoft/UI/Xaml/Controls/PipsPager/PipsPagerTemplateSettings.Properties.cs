using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PipsPagerTemplateSettings : DependencyObject
	{
		public IList<object> PipsPagerItems
		{
			get => (IList<object>)GetValue(PipsPagerItemsProperty);
			set => SetValue(PipsPagerItemsProperty, value);
		}

		public static DependencyProperty PipsPagerItemsProperty { get; } =
			DependencyProperty.Register(nameof(PipsPagerItems), typeof(IList<object>), typeof(PipsPagerTemplateSettings), new PropertyMetadata(null));
	}
}
