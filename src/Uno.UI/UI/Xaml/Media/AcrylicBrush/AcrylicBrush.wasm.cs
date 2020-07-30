using System.Text;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
	{
		private const string CssSupportCondition =
			"(backdrop-filter: blur(20px)) or (-webkit-backdrop-filter: blur(20px))";

		private static bool? _isBackdropFilterSupported = null;

		internal string CheckCssString()
		{
			return $@"background-color: {FallbackColor.ToCssString()}
				@supports (backdrop-filter: blur(20px)) or (-webkit-backdrop-filter: blur(20px)) {{
					-webkit-backdrop-filter: blur(20px);
					backdrop-filter: blur(20px);
					background-color: {TintColor.ToCssString()};
				}}";
		}

		internal void SetStyle(UIElement element)
		{
			if (IsBackdropFilterSupported())
			{				
				// "real" acrylic
				element.SetStyle(
					("-webkit-backdrop-filter", "blur(20px)"),
					("backdrop-filter", "blur(20px)"),
					("background-color", TintColor.ToCssString()));
			}
			else
			{
				// Set fallback color instead
				element.SetStyle("background-color", FallbackColor.ToCssString());
			}
		}

		internal static void ResetStyle(UIElement element)
		{
			element.ResetStyle(
				"-webkit-backdrop-filter",
				"backdrop-filter",
				"background-color");
		}

		private static bool IsBackdropFilterSupported() =>
			_isBackdropFilterSupported ??= WindowManagerInterop.IsCssFeatureSupported(CssSupportCondition);
	}
}
