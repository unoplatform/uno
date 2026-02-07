using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Content))]
public partial class ContentControl : Control
{
	public override string GetAccessibilityInnerText()
	{
		switch (Content)
		{
			case string str:
				return str;
			case IFrameworkElement frameworkElement:
				return frameworkElement.GetAccessibilityInnerText();
			case object content:
				return content.ToString();
			default:
				return null;
		}
	}
#nullable enable

	internal void ClearContentPresenterBypass()
	{
		if (Content is UIElement contentAsUIE && ContentTemplateRoot == contentAsUIE)
		{
			RemoveChild(contentAsUIE);
#if !UNO_HAS_ENHANCED_LIFECYCLE
			ContentTemplateRoot = null;
#endif
		}
	}
}
