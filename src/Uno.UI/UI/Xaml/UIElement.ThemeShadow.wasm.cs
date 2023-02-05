namespace Windows.UI.Xaml;

public partial class UIElement
{
	private bool _isShadowSet;

	partial void UnsetShadow()
	{
		if (_isShadowSet)
		{
			this.SetStyle("box-shadow", "unset");
			_isShadowSet = false;
		}
	}

	partial void SetShadow()
	{
		var translation = Translation;
		var boxShadowValue = CreateBoxShadow(translation.Z);
		this.SetStyle("box-shadow", boxShadowValue);
		_isShadowSet = true;
	}

	private static string CreateBoxShadow(float translationZ)
	{
		var z = (int)translationZ / 2;
		var halfZ = z / 2;
		var quarterZ = z / 4;
		return $"{quarterZ}px {quarterZ}px {halfZ}px 0px rgba(0,0,0,0.1)";
	}
}
