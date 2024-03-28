#nullable enable

namespace Windows.UI.Xaml.Controls.Primitives;

public sealed partial class Thumb : Control
{
	/// <summary>
	/// Gets whether the Thumb control has focus and mouse capture.
	/// </summary>
	public bool IsDragging
	{
		get => (bool)GetValue(IsDraggingProperty);
		set => SetValue(IsDraggingProperty, value);
	}

	/// <summary>
	/// Identifies the IsDragging dependency property.
	/// </summary>
	public static DependencyProperty IsDraggingProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDragging),
			typeof(bool),
			typeof(Thumb),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Fires when the Thumb control loses mouse capture.
	/// </summary>
	public event DragCompletedEventHandler? DragCompleted;

	/// <summary>
	/// Fires one or more times as the mouse pointer is moved when a Thumb control has logical focus and mouse capture.
	/// </summary>
	public event DragDeltaEventHandler? DragDelta;

	/// <summary>
	/// Fires when a Thumb control receives logical focus and mouse capture.
	/// </summary>
	public event DragStartedEventHandler? DragStarted;
}
