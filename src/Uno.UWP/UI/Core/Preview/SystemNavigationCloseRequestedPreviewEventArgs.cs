#if __MACOS__ || __SKIA__
using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.UI.Core.Preview;

public partial class SystemNavigationCloseRequestedPreviewEventArgs
{
	internal SystemNavigationCloseRequestedPreviewEventArgs(Action<SystemNavigationCloseRequestedPreviewEventArgs> complete)
	{
		DeferralManager = new(h => new Deferral(h));
		DeferralManager.Completed += (s, e) => complete?.Invoke(this);
	}

	public bool Handled { get; set; }

	internal DeferralManager<Deferral> DeferralManager { get; } = null!;

	public Deferral GetDeferral() => DeferralManager.GetDeferral();
}
#endif
