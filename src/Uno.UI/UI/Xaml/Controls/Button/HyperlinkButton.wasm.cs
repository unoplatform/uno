using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class HyperlinkButton
	{
		partial void PartialInitializeProperties()
		{
			if (!FeatureConfiguration.ButtonBase.UseHandCursor)
			{
				// If we turned off pointers for button base via configuration
				// we need to re-enable them for HyperlinkButton manually.
				SetStyle("cursor", "pointer");
			}
		}
	}
}
