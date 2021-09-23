#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Provider
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISelectionItemProvider 
	{
		#if false
		bool IsSelected
		{
			get;
		}
		#endif
		#if false
		global::Windows.UI.Xaml.Automation.Provider.IRawElementProviderSimple SelectionContainer
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.ISelectionItemProvider.IsSelected.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.ISelectionItemProvider.SelectionContainer.get
		#if false
		void AddToSelection();
		#endif
		#if false
		void RemoveFromSelection();
		#endif
		#if false
		void Select();
		#endif
	}
}
