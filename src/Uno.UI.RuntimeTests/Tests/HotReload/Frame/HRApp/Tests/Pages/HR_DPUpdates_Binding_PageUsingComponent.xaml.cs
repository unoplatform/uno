using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

public sealed partial class HR_DPUpdates_Binding_PageUsingComponent : Page
{
	public HR_DPUpdates_Binding_PageUsingComponent()
	{
		this.InitializeComponent();
		myComponent.DataContext = this;
	}

	public object Tag2
	{
		get => GetValue(Tag2Property);
		set => SetValue(Tag2Property, value);
	}

	public static DependencyProperty Tag2Property { get; } =
		DependencyProperty.Register(nameof(Tag2), typeof(object), typeof(HR_DPUpdates_Binding_PageUsingComponent), new PropertyMetadata(defaultValue: null));
}
