using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents a specialized command bar used in a CommandBarFlyout.
/// </summary>
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

	/// <summary>
	/// Gets or sets the system backdrop to apply to this CommandBar flyout.
	/// The backdrop is rendered behind the CommandBar flyout content.
	/// </summary>
	public SystemBackdrop SystemBackdrop
	{
		get => (SystemBackdrop)this.GetValue(SystemBackdropProperty);
		set => this.SetValue(SystemBackdropProperty, value);
	}

	/// <summary>
	/// Identifies the SystemBackdrop dependency property.
	/// </summary>
	public static DependencyProperty SystemBackdropProperty { get; } =
		DependencyProperty.Register(
			nameof(SystemBackdrop),
			typeof(SystemBackdrop),
			typeof(CommandBarFlyoutCommandBar),
			new FrameworkPropertyMetadata(default(SystemBackdrop), (s, e) => ((CommandBarFlyoutCommandBar)s).OnPropertyChanged(e)));
}
