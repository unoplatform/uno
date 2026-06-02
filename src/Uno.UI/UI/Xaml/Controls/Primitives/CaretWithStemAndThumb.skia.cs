using System;
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
	private static readonly Color ThumbFillColor = Colors.FromARGB("FF0078D7");

	private readonly Action _repositionCallback;
	private readonly Rectangle _stem;
	private Popup _popup;

	public PointerPoint LastPointerDown { get; set; }

	/// <param name="repositionCallback">
	/// Invoked once per rendered frame while the gripper is showing, so the owner
	/// can keep the gripper glued to its anchor character as the text moves.
	/// </param>
	public CaretWithStemAndThumb(Action  repositionCallback)
	{
		_repositionCallback = repositionCallback;
		// Numbers and colors below are partially measured by hand from WinUI and partially made up to be reasonable.

		Background = new SolidColorBrush(Colors.Transparent); // to hit-test positively everywhere in the grid

		Width = 16;

		RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		RowDefinitions.Add(new RowDefinition { Height = new GridLength(16, GridUnitType.Pixel) });

		var thumb = new Ellipse
		{
			Fill = new SolidColorBrush(Colors.White),
			Width = 16,
			Height = 16
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
			_popup.Closed += OnPopupClosed;
			((CompositionTarget)Visual.CompositionTarget)!.FrameRendered += OnFrameRendered;
		}
	}

	private void OnPopupClosed(object sender, object e)
	{
		_popup.Closed -= OnPopupClosed;
		if (Visual.CompositionTarget is CompositionTarget target)
		{
			target.FrameRendered -= OnFrameRendered;
		}
	}

	private void OnFrameRendered() => _repositionCallback?.Invoke();

	public void Hide()
	{
		if (_popup is not null)
		{
			_popup.IsOpen = false;
		}
	}
}
