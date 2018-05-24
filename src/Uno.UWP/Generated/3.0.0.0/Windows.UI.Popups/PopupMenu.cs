#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Popups
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PopupMenu 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Popups.IUICommand> Commands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<IUICommand> PopupMenu.Commands is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PopupMenu() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Popups.PopupMenu", "PopupMenu.PopupMenu()");
		}
		#endif
		// Forced skipping of method Windows.UI.Popups.PopupMenu.PopupMenu()
		// Forced skipping of method Windows.UI.Popups.PopupMenu.Commands.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Popups.IUICommand> ShowAsync( global::Windows.Foundation.Point invocationPoint)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IUICommand> PopupMenu.ShowAsync(Point invocationPoint) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Popups.IUICommand> ShowForSelectionAsync( global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IUICommand> PopupMenu.ShowForSelectionAsync(Rect selection) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Popups.IUICommand> ShowForSelectionAsync( global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IUICommand> PopupMenu.ShowForSelectionAsync(Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
	}
}
