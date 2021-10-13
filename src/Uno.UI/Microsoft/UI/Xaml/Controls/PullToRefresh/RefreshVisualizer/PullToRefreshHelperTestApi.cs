using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Private.Controls
{
	public static partial class PullToRefreshHelperTestApi
	{
		public static RefreshInteractionRatioChangedEventArgs CreateRefreshInteractionRatioChangedEventArgsInstance(double value)
		{
			return new RefreshInteractionRatioChangedEventArgs(value);
		}

		public static RefreshStateChangedEventArgs CreateRefreshStateChangedEventArgsInstance(RefreshVisualizerState oldValue, RefreshVisualizerState newValue)
		{
			return new RefreshStateChangedEventArgs(oldValue, newValue);
		}

		public static RefreshRequestedEventArgs CreateRefreshRequestedEventArgsInstance(Deferral handler)
		{
			return new RefreshRequestedEventArgs(handler);
		}
	}
}
