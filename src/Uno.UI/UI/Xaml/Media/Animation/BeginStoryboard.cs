namespace Microsoft.UI.Xaml.Media.Animation;

public partial class BeginStoryboard : TriggerAction
{
	public BeginStoryboard()
	{
	}

	public Storyboard Storyboard
	{
		get => (Storyboard)GetValue(StoryboardProperty);
		set => SetValue(StoryboardProperty, value);
	}

	public static DependencyProperty StoryboardProperty { get; } =
		DependencyProperty.Register(
			nameof(Storyboard),
			typeof(Storyboard),
			typeof(BeginStoryboard),
			new FrameworkPropertyMetadata(default(Storyboard)));
}
