#nullable enable

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Composition;
using RefreshPullDirection = Microsoft.UI.Xaml.Controls.RefreshPullDirection;

namespace Microsoft.UI.Private.Controls;

internal partial class RefreshInfoProviderImpl
{
	private const double DEFAULT_EXECUTION_RATIO = 0.8;

	private RefreshPullDirection m_refreshPullDirection = RefreshPullDirection.TopToBottom;
	private Size m_refreshVisualizerSize = new Size(1.0f, 1.0f);
	private bool m_isInteractingForRefresh = false;
	private int m_interactionRatioChangedCount = 0;
	private CompositionPropertySet? m_compositionProperties = null;
	private string m_interactionRatioCompositionProperty = "InteractionRatio";
	private double m_executionRatio = DEFAULT_EXECUTION_RATIO;
	private bool m_peeking = false;

	public event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;
	public event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;
	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;
	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;
}
