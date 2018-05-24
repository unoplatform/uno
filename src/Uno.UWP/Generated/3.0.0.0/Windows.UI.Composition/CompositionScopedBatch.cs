#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionScopedBatch : global::Windows.UI.Composition.CompositionObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsActive
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CompositionScopedBatch.IsActive is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsEnded
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CompositionScopedBatch.IsEnded is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionScopedBatch.IsActive.get
		// Forced skipping of method Windows.UI.Composition.CompositionScopedBatch.IsEnded.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void End()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionScopedBatch", "void CompositionScopedBatch.End()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Resume()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionScopedBatch", "void CompositionScopedBatch.Resume()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Suspend()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionScopedBatch", "void CompositionScopedBatch.Suspend()");
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionScopedBatch.Completed.add
		// Forced skipping of method Windows.UI.Composition.CompositionScopedBatch.Completed.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Composition.CompositionBatchCompletedEventArgs> Completed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionScopedBatch", "event TypedEventHandler<object, CompositionBatchCompletedEventArgs> CompositionScopedBatch.Completed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionScopedBatch", "event TypedEventHandler<object, CompositionBatchCompletedEventArgs> CompositionScopedBatch.Completed");
			}
		}
		#endif
	}
}
