#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AnnotationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpellingError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GrammarError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Comment,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FormulaError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrackChanges,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Header,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Footer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Highlighted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Endnote,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Footnote,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InsertionChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeletionChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MoveChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FormatChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsyncedChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EditingLockedChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExternalChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConflictingChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Author,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AdvancedProofingIssue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataValidationError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CircularReferenceError,
		#endif
	}
	#endif
}
