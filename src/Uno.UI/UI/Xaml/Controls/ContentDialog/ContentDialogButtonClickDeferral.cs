using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogButtonClickDeferral
	{
		private readonly Action _deferralAction;

		internal ContentDialogButtonClickDeferral(Action deferralAction)
		{
			_deferralAction = deferralAction;
		}

		public void Complete() => _deferralAction?.Invoke();
	}
}
