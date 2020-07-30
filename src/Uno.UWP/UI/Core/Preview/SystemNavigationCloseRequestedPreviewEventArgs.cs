#if __MACOS__
using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.UI.Core.Preview
{
	public partial class SystemNavigationCloseRequestedPreviewEventArgs
	{
		private DeferralManager<Deferral> _deferralManager;
		private readonly Action<SystemNavigationCloseRequestedPreviewEventArgs> _complete;

		public SystemNavigationCloseRequestedPreviewEventArgs(Action<SystemNavigationCloseRequestedPreviewEventArgs> complete)
		{
			_complete = complete;
		}

		public bool Handled { get; set; }

		internal bool IsDeferred => _deferralManager != null;

		internal void EventRaiseCompleted() => _deferralManager?.EventRaiseCompleted();

		public Deferral GetDeferral()
		{
			if (_deferralManager == null)
			{
				_deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));
				_deferralManager.Completed += (s, e) => _complete(this);
			}

			return _deferralManager.GetDeferral();
		}		
	}
}
#endif
