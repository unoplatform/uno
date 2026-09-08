namespace Microsoft.UI.Xaml.Controls;

public partial class RichEditBox : IHighContrastAdjustmentAware
{
	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		base.ApplyTemplate(out addedVisuals);

		if (addedVisuals)
		{
			UpdateHighContrastBackgroundOverride();
		}
	}

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
