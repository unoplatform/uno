using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollViewer
{
	// Hooks the WinUI port's template-part wiring on top of the cross-platform
	// OnApplyTemplate. See ScrollViewer.partial.mux.cs for the implementation.
	partial void OnApplyTemplatePartial() => OnApplyTemplate_MuxPartial();

	private (double? horizontal, double? vertical) ClampOffsetsToFocusedTextBox(double? horizontalOffset, double? verticalOffset)
	{
		if (Presenter is not null && ShouldSnapToTouchTextBox())
		{
			var textBox = (TextBox)FocusManager.GetFocusedElement(XamlRoot!)!;
			var textBoxToPresenter = textBox.TransformToVisual(Presenter.FindFirstChild()).TransformBounds(new Rect(0, 0, textBox.ActualWidth, textBox.ActualHeight));
			if (verticalOffset.HasValue)
			{
				if (verticalOffset + ViewportHeight < textBoxToPresenter.Top)
				{
					verticalOffset = textBoxToPresenter.Top - ViewportHeight + textBoxToPresenter.Height;
				}
				else if (verticalOffset > textBoxToPresenter.Bottom)
				{
					verticalOffset = textBoxToPresenter.Top;
				}
			}

			if (horizontalOffset.HasValue)
			{
				if (horizontalOffset + ViewportWidth < textBoxToPresenter.Left)
				{
					horizontalOffset = textBoxToPresenter.Left - ViewportWidth + textBoxToPresenter.Width;
				}
				else if (horizontalOffset > textBoxToPresenter.Right)
				{
					horizontalOffset = textBoxToPresenter.Left;
				}
			}
		}

		return (horizontalOffset, verticalOffset);
	}

	internal partial bool ShouldSnapToTouchTextBox()
	{
		return XamlRoot is not null && FocusManager.GetFocusedElement(XamlRoot) is TextBox { CaretMode: TextBox.CaretDisplayMode.CaretWithThumbsBothEndsShowing or TextBox.CaretDisplayMode.CaretWithThumbsOnlyEndShowing } textBox && textBox.FindFirstParent<ScrollViewer>() == this;
	}
}
