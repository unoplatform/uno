#if __IOS__ || __ANDROID__
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	public RefreshContainer()
	{
		DefaultStyleKey = typeof(RefreshContainer);

		InitializePlatform();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_refreshVisualizer = Visualizer;
		if (m_refreshVisualizer == null)
		{
			Visualizer = new RefreshVisualizer();
			m_hasDefaultRefreshVisualizer = true;
		}
		else
		{
			m_hasDefaultRefreshVisualizer = false;
		}

		OnApplyTemplatePartial();
	}

	partial void OnApplyTemplatePartial();

	/// <summary>
	/// Initiates an update of the content.
	/// </summary>
	public void RequestRefresh() => RequestRefreshPlatform();
	
	private void OnNativeRefreshingChanged()
	{
		if (IsNativeRefreshing && Visualizer?.State != RefreshVisualizerState.Refreshing)
		{
			var deferral = new Deferral(() =>
			{
				// CheckThread();
				EndNativeRefreshing();
				RefreshCompleted();
			});

			var args = new RefreshRequestedEventArgs(deferral);

			//This makes sure that everyone registered for this event can get access to the deferral
			//Otherwise someone could complete the deferral before someone else has had a chance to grab it
			args.IncrementDeferralCount();
			RefreshRequested?.Invoke(this, args);
			args.DecrementDeferralCount();
		}
	}
}
#endif
