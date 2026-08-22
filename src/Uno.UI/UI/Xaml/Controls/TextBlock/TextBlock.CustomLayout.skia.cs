#nullable enable

using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Controls;

partial class TextBlock
{
	private ICustomTextLayout? _customTextLayout;

	internal ICustomTextLayout? CustomTextLayout
	{
		get => _customTextLayout;
		set
		{
			if (!ReferenceEquals(_customTextLayout, value))
			{
				_customTextLayout = value;
				InvalidateMeasure();
			}
		}
	}
}
