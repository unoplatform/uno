#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkCanvas : global::Windows.UI.Xaml.FrameworkElement
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.Inking.InkPresenter InkPresenter
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkPresenter InkCanvas.InkPresenter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public InkCanvas() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.InkCanvas", "InkCanvas.InkCanvas()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.InkCanvas.InkCanvas()
		// Forced skipping of method Windows.UI.Xaml.Controls.InkCanvas.InkPresenter.get
	}
}
