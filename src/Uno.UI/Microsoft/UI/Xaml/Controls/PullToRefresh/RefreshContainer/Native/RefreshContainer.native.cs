#if __IOS__ || __ANDROID__
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

		OnApplyTemplatePartial();
	}

	partial void OnApplyTemplatePartial();

	/// <summary>
	/// Initiates an update of the content.
	/// </summary>
	public void RequestRefresh() => RequestRefreshPlatform();

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{		
	}	
}
#endif
