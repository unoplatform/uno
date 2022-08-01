using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Composition;

namespace Uno.UI.Xaml.Controls;

internal class NativeRefreshInfoProvider : IRefreshInfoProvider
{
	private readonly RefreshContainer _refreshContainer;

	public NativeRefreshInfoProvider(RefreshContainer refreshContainer)
	{
		_refreshContainer = refreshContainer;
	}

	public bool IsInteractingForRefresh { get; }

	public CompositionPropertySet CompositionProperties => null;

	public string InteractionRatioCompositionProperty => null;

	public double ExecutionRatio => 1;

	public event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;

	public event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;

	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;

	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;

	public void OnRefreshCompleted() => RefreshCompleted?.Invoke(this, null);

	public void OnRefreshStarted() => RefreshStarted?.Invoke(this, null);
}
