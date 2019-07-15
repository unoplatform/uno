using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogClosingDeferral
	{
		private readonly Action _deferralAction;

		public ContentDialogClosingDeferral(Action deferralAction)
			=> _deferralAction = deferralAction;

		public void Complete() => _deferralAction?.Invoke();
	}
}
