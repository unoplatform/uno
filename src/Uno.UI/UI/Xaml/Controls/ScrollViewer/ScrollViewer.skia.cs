namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollViewer
{
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
