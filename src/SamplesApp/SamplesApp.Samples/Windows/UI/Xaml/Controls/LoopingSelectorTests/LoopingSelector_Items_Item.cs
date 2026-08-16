using Microsoft.UI.Xaml;

namespace UITests.Windows_UI_Xaml_Controls.LoopingSelectorTests
{
	public partial class LoopingSelector_Items_Item : DependencyObject
	{

		public string PrimaryText
		{
			get => (string)GetValue(PrimaryTextProperty);
			set => SetValue(PrimaryTextProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty PrimaryTextProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(PrimaryText), typeof(string),
				typeof(LoopingSelector_Items_Item),
				new PropertyMetadata("default"));

		public override string ToString()
		{
			return PrimaryText;
		}
	}
}
