using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class CommandBarFlyoutCommandBar : CommandBar
{
	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding} markup
	/// extension sources when defining templates for a CommandBarFlyoutCommandBar control.
	/// </summary>
	public CommandBarFlyoutCommandBarTemplateSettings FlyoutTemplateSettings
	{
		get => (CommandBarFlyoutCommandBarTemplateSettings)GetValue(FlyoutTemplateSettingsProperty);
		set => SetValue(FlyoutTemplateSettingsProperty, value);
	}

	internal static DependencyProperty FlyoutTemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(FlyoutTemplateSettings),
				typeof(CommandBarFlyoutCommandBarTemplateSettings),
				typeof(CommandBarFlyoutCommandBar),
				new FrameworkPropertyMetadata(null));
}
