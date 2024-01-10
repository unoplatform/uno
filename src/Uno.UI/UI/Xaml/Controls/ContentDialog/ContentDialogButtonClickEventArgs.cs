using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentDialogButtonClickEventArgs
	{
		private readonly Action<ContentDialogButtonClickEventArgs> _deferralAction;

		internal ContentDialogButtonClickEventArgs(Action<ContentDialogButtonClickEventArgs> deferralAction)
		{
			_deferralAction = deferralAction;
		}

		public bool Cancel { get; set; }
		internal ContentDialogButtonClickDeferral Deferral { get; private set; }

		public global::Microsoft.UI.Xaml.Controls.ContentDialogButtonClickDeferral GetDeferral()
			=> Deferral = new ContentDialogButtonClickDeferral(() => _deferralAction(this));
	}
}
