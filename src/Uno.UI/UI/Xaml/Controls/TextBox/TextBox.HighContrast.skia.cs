namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox : IHighContrastAdjustmentAware
{
	void IHighContrastAdjustmentAware.OnHighContrastAdjustmentChanged() =>
		UpdateHighContrastBackgroundOverride();

	private void UpdateHighContrastBackgroundOverride()
	{
		if (GetTemplateChild("BorderElement") is Border border)
		{
			border.SetUseBackgroundOverride(IsHighContrastAdjustmentEnabled());
		}
	}
}
