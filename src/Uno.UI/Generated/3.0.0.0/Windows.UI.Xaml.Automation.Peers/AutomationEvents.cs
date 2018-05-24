#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationEvents 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToolTipOpened,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToolTipClosed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MenuOpened,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MenuClosed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AutomationFocusChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvokePatternOnInvoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectionItemPatternOnElementAddedToSelection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectionItemPatternOnElementRemovedFromSelection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectionItemPatternOnElementSelected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectionPatternOnInvalidated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TextPatternOnTextSelectionChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TextPatternOnTextChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AsyncContentLoaded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PropertyChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StructureChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DragStart,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DragCancel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DragComplete,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DragEnter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DragLeave,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dropped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LiveRegionChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InputReachedTarget,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InputReachedOtherElement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InputDiscarded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowClosed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowOpened,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConversionTargetChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TextEditTextChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LayoutInvalidated,
		#endif
	}
	#endif
}
