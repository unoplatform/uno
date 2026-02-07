using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollViewer
{
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

	partial void OnZoomModeChangedPartial(ZoomMode zoomMode)
	{
		if (_presenter is ScrollContentPresenter scp)
		{
			switch (zoomMode)
			{
				case ZoomMode.Disabled:
					// When zoom is disabled, set min/max to 1 to prevent any zooming
					scp.OnMinZoomFactorChanged(1f);
					scp.OnMaxZoomFactorChanged(1f);
					break;
				case ZoomMode.Enabled:
					// When zoom is enabled, use the actual min/max values
					scp.OnMinZoomFactorChanged(MinZoomFactor);
					scp.OnMaxZoomFactorChanged(MaxZoomFactor);
					break;
			}
		}
	}
}
