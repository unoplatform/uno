#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

internal partial class DisplayMemberTemplate : DataTemplate
{
	internal override UIElement? LoadContent(FrameworkElement templatedParent)
	{
		if (templatedParent is ContentPresenter cp)
		{
			return cp.CreateDefaultContent();
		}
		else
		{
			var template = ContentControl.CreateDefaultTemplate(templatedParent);
			return template.LoadContent(templatedParent);
		}
	}
}
