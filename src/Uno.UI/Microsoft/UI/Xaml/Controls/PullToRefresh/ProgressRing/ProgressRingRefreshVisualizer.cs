using Microsoft.UI.Xaml.Controls;

using WUXProgressRing = Microsoft.UI.Xaml.Controls.ProgressRing;

namespace Uno.UI.Xaml.Controls;

internal partial class ProgressRingRefreshVisualizer : RefreshVisualizer
{
	public ProgressRingRefreshVisualizer()
	{
		Content = ProgressRing;
	}

	internal WUXProgressRing ProgressRing { get; } = new WUXProgressRing();
}
