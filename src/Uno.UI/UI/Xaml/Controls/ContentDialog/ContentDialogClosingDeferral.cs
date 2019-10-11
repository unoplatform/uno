using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogClosingDeferral : IDeferral
	{
		private Action _deferralAction;

		public ContentDialogClosingDeferral() { }

		Action IDeferral.DeferralAction { get => _deferralAction; set => _deferralAction = value; }

		public void Complete() => _deferralAction?.Invoke();
	}
}
