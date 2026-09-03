using System.Numerics;
using Windows.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// The draggable touch-selection gripper used by text controls (TextBox and selectable TextBlock).
/// It renders a thumb (the draggable knob), a ring around it and an optional stem that connects
/// the thumb to the text while dragging. It hosts itself in a <see cref="Popup"/> so it can be
/// rendered above the rest of the visual tree.
/// </summary>
internal sealed class CaretWithStemAndThumb : Grid
{
	// This is equal to the default system accent color on Windows.
	// This is, however, a constant color that doesn't depend on the
	// current system accent color. Changing the accent color does NOT
	// change the thumb color on WinUI, only the selection color.
	internal static readonly Color ThumbFillColor = Colors.FromARGB("FF0078D7");

	/// <summary>
	/// The side of the (square) thumb. The gripper is this wide, and hangs this far below the caret line
	/// it points at, which is what <see cref="TextSelectionGripperPresenter"/> culls against.
	/// </summary>
	internal const double ThumbSize = 16;

	private readonly Rectangle _stem;
	private Popup _popup;

	public PointerPoint LastPointerDown { get; set; }

	/// <summary>
	/// Vertical distance (in the text surface's coordinates) from the finger to the caret line it points at,
	/// captured when the gripper is pressed. The thumb hangs below the line, so the drag must subtract this to
	/// sample the text on the caret's own line instead of the one below it. See TextSelectionGripperPresenter.
	/// </summary>
	public double GrabOffsetY { get; set; }

	public CaretWithStemAndThumb()
	{
		// Numbers and colors below are partially measured by hand from WinUI and partially made up to be reasonable.

		Background = new SolidColorBrush(Colors.Transparent); // to hit-test positively everywhere in the grid

		Width = ThumbSize;

		RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		RowDefinitions.Add(new RowDefinition { Height = new GridLength(ThumbSize, GridUnitType.Pixel) });

		var thumb = new Ellipse
		{
			Fill = new SolidColorBrush(Colors.White),
			Width = ThumbSize,
			Height = ThumbSize
		};

		var thumbRing = new Ellipse
		{
			Stroke = new SolidColorBrush(ThumbFillColor),
			StrokeThickness = 2,
			Width = 14,
			Height = 14,
			Margin = new Thickness(1)
		};

		_stem = new Rectangle
		{
			Visibility = Visibility.Collapsed,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			Stroke = new SolidColorBrush(ThumbFillColor),
			Width = 2
		};

		Grid.SetRow(_stem, 0);
		Grid.SetRow(thumb, 1);
		Grid.SetRow(thumbRing, 1);

		Children.Add(_stem);
		Children.Add(thumb);
		Children.Add(thumbRing);
	}

	// Test hook: whether the gripper is actually painted. Its measured size survives a hide, so size alone
	// is not a reliable signal.
	internal bool IsShowing => _popup?.IsOpen is true;

	public void SetStemVisible(bool visible) => _stem.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

	public void ShowAt(XamlRoot xamlRoot, Matrix3x2 transform)
	{
		_popup ??= new Popup
		{
			Child = this,
			IsLightDismissEnabled = false,
			XamlRoot = xamlRoot
		};
		_popup.PopupPanel.Visual.ZIndex = VisualTree.TextBoxTouchKnobPopupZIndex;

		if (RenderTransform is not MatrixTransform matrixTransform)
		{
			matrixTransform = new MatrixTransform();
			RenderTransform = matrixTransform;
		}
		matrixTransform.Matrix = new Matrix(transform);
		if (!_popup.IsOpen)
		{
			_popup.IsOpen = true;
		}
	}

	public void Hide()
	{
		if (_popup is not null)
		{
			_popup.IsOpen = false;
		}
	}
}
