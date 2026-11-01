#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ObjectAnimationUsingKeyFrames
{
	/// <summary>
	/// Gets the collection of ObjectKeyFrame objects that define the animation.
	/// </summary>
	public ObjectKeyFrameCollection KeyFrames
	{
		get => (ObjectKeyFrameCollection)GetValue(KeyFramesProperty);
		internal set => SetValue(KeyFramesProperty, value);
	}

	/// <remarks>
	/// Not a DP in WinUI, but it must be one here for the DataContext/TemplatedParent to flow to the key frames.
	/// </remarks>
	internal static DependencyProperty KeyFramesProperty { get; } =
		DependencyProperty.Register(
			name: nameof(KeyFrames),
			propertyType: typeof(ObjectKeyFrameCollection),
			ownerType: typeof(ObjectAnimationUsingKeyFrames),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: null));

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
		DependencyProperty.Register(nameof(EnableDependentAnimation), typeof(bool), typeof(ObjectAnimationUsingKeyFrames), new FrameworkPropertyMetadata(BoolBoxes.False));
}
