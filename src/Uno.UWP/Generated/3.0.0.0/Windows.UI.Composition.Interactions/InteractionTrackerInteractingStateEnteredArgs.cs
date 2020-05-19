#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition.Interactions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InteractionTrackerInteractingStateEnteredArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int RequestId
		{
			get
			{
				throw new global::System.NotImplementedException("The member int InteractionTrackerInteractingStateEnteredArgs.RequestId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsFromBinding
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InteractionTrackerInteractingStateEnteredArgs.IsFromBinding is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.Interactions.InteractionTrackerInteractingStateEnteredArgs.RequestId.get
		// Forced skipping of method Windows.UI.Composition.Interactions.InteractionTrackerInteractingStateEnteredArgs.IsFromBinding.get
	}
}
