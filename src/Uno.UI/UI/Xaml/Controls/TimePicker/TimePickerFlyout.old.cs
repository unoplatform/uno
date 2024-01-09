//#if !UNO_REFERENCE_API
//using System;
//using Windows.Foundation;
//using Microsoft.UI.Xaml.Controls.Primitives;

//namespace Microsoft.UI.Xaml.Controls;

//partial class TimePickerFlyout
//{
//#if !__ANDROID__ && !__IOS__
//	protected override Control CreatePresenter() => throw new NotImplementedException();
//#endif

//#if __ANDROID__ || __IOS__
//	public event TypedEventHandler<TimePickerFlyout, TimePickedEventArgs> TimePicked;
//#endif

//	protected override void OnConfirmed() => throw new NotImplementedException();

//	protected override bool ShouldShowConfirmationButtons() => throw new NotImplementedException();
//}
//#endif
