#nullable disable

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	partial class TimePickerFlyout : PickerFlyoutBase
	{
#if !__ANDROID__ && !__IOS__
		protected override Control CreatePresenter() => throw new NotImplementedException();
#endif

#if __ANDROID__ || __IOS__
		public event TypedEventHandler<TimePickerFlyout, TimePickedEventArgs> TimePicked;
#endif

		protected override void OnConfirmed() => throw new NotImplementedException();

		protected override bool ShouldShowConfirmationButtons() => throw new NotImplementedException();
	}
}
