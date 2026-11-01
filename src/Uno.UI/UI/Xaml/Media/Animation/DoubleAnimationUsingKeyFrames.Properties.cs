#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class DoubleAnimationUsingKeyFrames
{
	/// <summary>
	/// Gets the collection of DoubleKeyFrame objects that define the animation.
	/// </summary>
	public DoubleKeyFrameCollection KeyFrames { get; }

	/// <summary>
	/// Gets or sets a value that declares whether animated properties that are considered dependent animations should be
	/// permitted to use this animation declaration.
	/// </summary>
	public bool EnableDependentAnimation
	{
		get => (bool)GetValue(EnableDependentAnimationProperty);
		set => SetValue(EnableDependentAnimationProperty, value);
	}

	/// <summary>
	/// Identifies the EnableDependentAnimation dependency property.
	/// </summary>
	public static DependencyProperty EnableDependentAnimationProperty { get; } =
		DependencyProperty.Register(nameof(EnableDependentAnimation), typeof(bool), typeof(DoubleAnimationUsingKeyFrames), new FrameworkPropertyMetadata(BoolBoxes.False));
}
