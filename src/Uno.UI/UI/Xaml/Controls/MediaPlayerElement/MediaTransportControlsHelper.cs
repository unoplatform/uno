using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class MediaTransportControlsHelper
	{
		public static DependencyProperty DropoutOrderProperty
		{
			[DynamicDependency(nameof(GetDropoutOrder))]
			[DynamicDependency(nameof(SetDropoutOrder))]
			get;
		} = DependencyProperty.RegisterAttached(
				"DropoutOrder",
				typeof(int?),
				typeof(MediaTransportControlsHelper),
				new FrameworkPropertyMetadata(default(int?)));

		public static int? GetDropoutOrder(UIElement element)
		{
			return (int?)element.GetValue(DropoutOrderProperty);
		}

		public static void SetDropoutOrder(UIElement element, int? value)
		{
			element.SetValue(DropoutOrderProperty, value);
		}
	}
}
