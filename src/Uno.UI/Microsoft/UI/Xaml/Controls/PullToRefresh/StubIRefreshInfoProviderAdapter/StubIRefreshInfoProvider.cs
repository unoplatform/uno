#pragma warning disable CS0067

using Microsoft.UI.Private.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Composition;

namespace Uno.UI.Xaml.Controls;

internal class StubIRefreshInfoProvider : IRefreshInfoProvider
{
	public bool IsInteractingForRefresh => false;

	public CompositionPropertySet CompositionProperties => null;

	public string InteractionRatioCompositionProperty => null;

	public double ExecutionRatio => 1;

	public event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;

	public event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;

	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;

	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;

	public void OnRefreshCompleted()
	{
	}

	public void OnRefreshStarted()
	{
	}
}
