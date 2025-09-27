using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal partial class ProgressRingRefreshInfoProviderAdapter : IRefreshInfoProviderAdapter
{
	private readonly RefreshContainer _refreshContainer;
	private IRefreshInfoProvider _refreshInfoProvider;
	private ProgressRingRefreshVisualizer _progressRingVisualizer;

	public ProgressRingRefreshInfoProviderAdapter(RefreshContainer refreshContainer)
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
		_progressRingVisualizer.Visibility = Visibility.Visible;
		_progressRingVisualizer.ProgressRing.Visibility = Visibility.Visible;
		_progressRingVisualizer.ProgressRing.IsActive = true;
	}

	private void OnRefreshCompleted(IRefreshInfoProvider sender, object args)
	{
		_progressRingVisualizer.Visibility = Visibility.Collapsed;
		_progressRingVisualizer.ProgressRing.Visibility = Visibility.Collapsed;
		_progressRingVisualizer.ProgressRing.IsActive = false;
	}

	public void SetAnimations(UIElement refreshVisualizerAnimatableContainer)
	{
		if (refreshVisualizerAnimatableContainer is ProgressRingRefreshVisualizer progressRingVisualizer)
		{
			if (_progressRingVisualizer != progressRingVisualizer)
			{
				progressRingVisualizer.Visibility = Visibility.Collapsed;
				progressRingVisualizer.ProgressRing.IsActive = false;
				_progressRingVisualizer = progressRingVisualizer;
			}
		}
		else
		{
			_progressRingVisualizer = null;
		}
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
