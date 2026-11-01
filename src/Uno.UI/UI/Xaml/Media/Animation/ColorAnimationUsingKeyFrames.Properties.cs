#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ColorAnimationUsingKeyFrames
{
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
		DependencyProperty.Register(nameof(EnableDependentAnimation), typeof(bool), typeof(ColorAnimationUsingKeyFrames), new FrameworkPropertyMetadata(BoolBoxes.False));

	/// <summary>
	/// Identifies the KeyFrames dependency property.
	/// </summary>
	/// <remarks>Not a dependency property in WinUI.</remarks>
	public static DependencyProperty KeyFramesProperty { get; } =
		DependencyProperty.Register(nameof(KeyFrames), typeof(ColorKeyFrameCollection), typeof(ColorAnimationUsingKeyFrames), new FrameworkPropertyMetadata(default(ColorKeyFrameCollection), OnKeyFramesChanged));

	/// <summary>
	/// Gets the collection of ColorKeyFrame objects that define the animation.
	/// </summary>
	public ColorKeyFrameCollection KeyFrames
	{
		get => (ColorKeyFrameCollection)GetValue(KeyFramesProperty);
		internal set => SetValue(KeyFramesProperty, value);
	}

	private static void OnKeyFramesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is ColorAnimationUsingKeyFrames owner)
		{
			(e.OldValue as ColorKeyFrameCollection)?.SetParent(null);

			// The parent must always be set, so that items in the collection can use that to walk up the visual tree.
			// This is needed when updating the resource bindings that point to local resources (for example, from Page.Resources).
			(e.NewValue as ColorKeyFrameCollection)?.SetParent(owner);
		}
	}
}
