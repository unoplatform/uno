using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{
	public sealed partial class MarkupExtensionReturnTypeShouldBeIgnored : Page
	{
		public MarkupExtensionReturnTypeShouldBeIgnored()
		{
			this.InitializeComponent();
		}
	}

	[MarkupExtensionReturnType(ReturnType = typeof(int))]
	public class FontIcon : MarkupExtension
	{
		public string Glyph { get; set; }

		protected override object ProvideValue()
		{
			return new Windows.UI.Xaml.Controls.FontIcon()
			{
				Glyph = Glyph,
				FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI")
			};
		}
	}
}
