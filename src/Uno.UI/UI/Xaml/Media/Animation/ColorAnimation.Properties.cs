#nullable enable

using Uno.UI.Helpers.Boxes;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ColorAnimation
{
	/// <summary>
	/// Gets or sets the ending value of the animation.
	/// </summary>
	public Color? To
	{
		get => (Color?)GetValue(ToProperty);
		set => SetValue(ToProperty, value);
	}

	/// <summary>
	/// Identifies the To dependency property.
	/// </summary>
	public static DependencyProperty ToProperty { get; } =
		DependencyProperty.Register(nameof(To), typeof(Color?), typeof(ColorAnimation), new FrameworkPropertyMetadata(null, (s, e) => ((ColorAnimation)s).OnAnimationValuePropertyChanged(AssignmentOperand.To, e.NewValue)));

	/// <summary>
	/// Gets or sets the starting value of the animation.
	/// </summary>
	public Color? From
	{
		get => (Color?)GetValue(FromProperty);
		set => SetValue(FromProperty, value);
	}

	/// <summary>
	/// Identifies the From dependency property.
	/// </summary>
	public static DependencyProperty FromProperty { get; } =
		DependencyProperty.Register(nameof(From), typeof(Color?), typeof(ColorAnimation), new FrameworkPropertyMetadata(null, (s, e) => ((ColorAnimation)s).OnAnimationValuePropertyChanged(AssignmentOperand.From, e.NewValue)));

	/// <summary>
	/// Gets or sets the total amount by which the animation changes its starting value.
	/// </summary>
	public Color? By
	{
		get => (Color?)GetValue(ByProperty);
		set => SetValue(ByProperty, value);
	}

	/// <summary>
	/// Identifies the By dependency property.
	/// </summary>
	public static DependencyProperty ByProperty { get; } =
		DependencyProperty.Register(nameof(By), typeof(Color?), typeof(ColorAnimation), new FrameworkPropertyMetadata(null, (s, e) => ((ColorAnimation)s).OnAnimationValuePropertyChanged(AssignmentOperand.By, e.NewValue)));

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
		DependencyProperty.Register(nameof(EnableDependentAnimation), typeof(bool), typeof(ColorAnimation), new FrameworkPropertyMetadata(BoolBoxes.False));

	/// <summary>
	/// Gets or sets the easing function applied to this animation.
	/// </summary>
	public EasingFunctionBase EasingFunction
	{
		get => (EasingFunctionBase)GetValue(EasingFunctionProperty);
		set => SetValue(EasingFunctionProperty, value);
	}

	/// <summary>
	/// Identifies the EasingFunction dependency property.
	/// </summary>
	public static DependencyProperty EasingFunctionProperty { get; } =
		DependencyProperty.Register(nameof(EasingFunction), typeof(EasingFunctionBase), typeof(ColorAnimation), new FrameworkPropertyMetadata(null));
}
