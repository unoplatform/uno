namespace Microsoft.UI.Xaml.Controls;

public partial class RichEditBox : IHighContrastAdjustmentAware
{
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		UpdateHighContrastBackgroundOverride();
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
