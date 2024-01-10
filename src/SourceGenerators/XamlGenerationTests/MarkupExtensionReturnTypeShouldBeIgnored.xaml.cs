using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

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
			return new Microsoft.UI.Xaml.Controls.FontIcon()
			{
				Glyph = Glyph,
				FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI")
			};
		}
	}
}
