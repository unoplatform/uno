using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation.TestPages;

public sealed partial class TemplateBindingAfterAnimationPage : Page
{
	public TemplateBindingAfterAnimationPage()
	{
		this.InitializeComponent();
	}
}

public partial class CustomButton : Button
{
	public TextBlock TextBlockTemplateChildBoundToForeground { get; private set; }
	public TextBlock TextBlockTemplateChildBoundToBackground { get; private set; }

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		TextBlockTemplateChildBoundToForeground = (TextBlock)this.GetTemplateChild("tb1");
		TextBlockTemplateChildBoundToBackground = (TextBlock)this.GetTemplateChild("tb2");
		VisualStateManager.GoToState(this, "MyState", false);
	}
}
