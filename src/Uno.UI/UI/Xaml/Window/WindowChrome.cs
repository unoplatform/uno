#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Controls;

internal sealed partial class WindowChrome : ContentControl
{
	public WindowChrome(Microsoft.UI.Xaml.Window parent)
	{
		DefaultStyleKey = typeof(WindowChrome);

		HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
		HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;

		IsTabStop = false;
		CaptionVisibility = Visibility.Visible;
	}

	public Visibility CaptionVisibility
	{
		get => (Visibility)GetValue(CaptionVisibilityProperty);
		set => SetValue(CaptionVisibilityProperty, value);
	}

	public static DependencyProperty CaptionVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(CaptionVisibility), typeof(Visibility), typeof(WindowChrome), new FrameworkPropertyMetadata(Visibility.Collapsed));

	// to apply min, max and close style definitions to custom titlebar,
	// one needs to apply Content Control style with key WindowChromeStyle defined in generic.xaml
	internal void ApplyStylingForMinMaxCloseButtons()
	{
		var style = (Style)Application.Current.Resources["WindowChromeStyle"];
		SetValue(StyleProperty, style);
	}

	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		// Fire XamlRoot.Changed
		var xamlIslandRoot = VisualTree.GetXamlIslandRootForElement(this);
		xamlIslandRoot!.ContentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.Content);
	}
}

