#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class PointAnimationUsingKeyFrames
{
	/// <summary>
	/// Gets the collection of PointKeyFrame objects that define the animation.
	/// </summary>
	public PointKeyFrameCollection KeyFrames { get; }

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
		DependencyProperty.Register(nameof(EnableDependentAnimation), typeof(bool), typeof(PointAnimationUsingKeyFrames), new FrameworkPropertyMetadata(BoolBoxes.False));
}
