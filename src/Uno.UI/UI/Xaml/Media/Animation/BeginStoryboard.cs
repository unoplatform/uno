namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// A trigger action that begins a Storyboard and distributes its animations to their targeted objects and properties.
/// </summary>
public partial class BeginStoryboard : TriggerAction
{
	/// <summary>
	/// Initializes a new instance of the BeginStoryboard class.
	/// </summary>
	public BeginStoryboard()
	{
	}

	/// <summary>
	/// Gets or sets the Storyboard that this BeginStoryboard starts.
	/// </summary>
	public Storyboard Storyboard
	{
		get => (Storyboard)GetValue(StoryboardProperty);
		set => SetValue(StoryboardProperty, value);
	}

	/// <summary>
	/// Identifies the Storyboard dependency property.
	/// </summary>
	public static DependencyProperty StoryboardProperty { get; } =
		DependencyProperty.Register(
			nameof(Storyboard),
			typeof(Storyboard),
			typeof(BeginStoryboard),
			new FrameworkPropertyMetadata(default(Storyboard)));
}
