#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Provider
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWindowProvider 
	{
#if false
		global::Windows.UI.Xaml.Automation.WindowInteractionState InteractionState
		{
			get;
		}
#endif
#if false
		bool IsModal
		{
			get;
		}
#endif
#if false
		bool IsTopmost
		{
			get;
		}
#endif
#if false
		bool Maximizable
		{
			get;
		}
#endif
#if false
		bool Minimizable
		{
			get;
		}
#endif
#if false
		global::Windows.UI.Xaml.Automation.WindowVisualState VisualState
		{
			get;
		}
#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IWindowProvider.IsModal.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IWindowProvider.IsTopmost.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IWindowProvider.Maximizable.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IWindowProvider.Minimizable.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IWindowProvider.InteractionState.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IWindowProvider.VisualState.get
#if false
		void Close();
#endif
#if false
		void SetVisualState( global::Windows.UI.Xaml.Automation.WindowVisualState state);
#endif
#if false
		bool WaitForInputIdle( int milliseconds);
#endif
	}
}
