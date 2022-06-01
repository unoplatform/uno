#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundTaskRegistration2 : global::Windows.ApplicationModel.Background.IBackgroundTaskRegistration
	{
		#if false
		global::Windows.ApplicationModel.Background.IBackgroundTrigger Trigger
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration2.Trigger.get
	}
}
