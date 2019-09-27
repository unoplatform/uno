using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogClosingEventArgs
	{
		private DeferralManager<ContentDialogClosingDeferral> _deferralManager;
		private readonly Action<ContentDialogClosingEventArgs> _complete;

		internal ContentDialogClosingEventArgs(Action<ContentDialogClosingEventArgs> complete, ContentDialogResult result)
		{
			_complete = complete;

			Result = result;
		}
		internal bool IsDeferred => _deferralManager != null;

		public bool Cancel { get; set; }

		public ContentDialogResult Result { get; }
		
		public ContentDialogClosingDeferral GetDeferral()
		{
			_deferralManager = _deferralManager ?? new DeferralManager<ContentDialogClosingDeferral>(() => _complete(this));

			return _deferralManager.GetDeferral();
		}

		internal void EventRaiseCompleted() => _deferralManager?.EventRaiseCompleted();
	}
}
