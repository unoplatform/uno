using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogClosingDeferral : IDeferral
	{
		private Action _deferralAction;

		Action IDeferral.DeferralAction { get => _deferralAction; set => _deferralAction = value; }

		public void Complete() => _deferralAction?.Invoke();
	}
}
