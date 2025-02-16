#if __ANDROID__ || __IOS__
using Microsoft.UI.Private.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal class NativeRefreshInfoProviderAdapter : IRefreshInfoProviderAdapter
{
	private readonly RefreshContainer _refreshContainer;
	private IRefreshInfoProvider _refreshInfoProvider;

	public NativeRefreshInfoProviderAdapter(RefreshContainer refreshContainer)
	{
		_refreshContainer = refreshContainer;
	}

	public IRefreshInfoProvider AdaptFromTree(UIElement root, Size visualizerSize)
	{
		_refreshInfoProvider = new RefreshInfoProviderImpl();
		_refreshInfoProvider.RefreshStarted += OnRefreshStarted;
		_refreshInfoProvider.RefreshCompleted += OnRefreshCompleted;
		return _refreshInfoProvider;
	}

	private void OnRefreshStarted(IRefreshInfoProvider sender, object args)
	{
		_refreshContainer.RequestRefreshPlatform();
	}

	private void OnRefreshCompleted(IRefreshInfoProvider sender, object args)
	{
		_refreshContainer.EndNativeRefreshing();
	}

	public void SetAnimations(UIElement refreshVisualizerAnimatableContainer)
	{
		// TODO: Make visible
		refreshVisualizerAnimatableContainer.Visibility = Visibility.Collapsed;
	}

	public void Dispose()
	{
		// Unsubscribe events.
		if (_refreshInfoProvider != null)
		{
			_refreshInfoProvider.RefreshStarted -= OnRefreshStarted;
			_refreshInfoProvider.RefreshCompleted -= OnRefreshCompleted;
		}
	}
}
#endif
