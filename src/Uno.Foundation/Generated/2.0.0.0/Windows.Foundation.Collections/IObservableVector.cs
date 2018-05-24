#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Collections
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IObservableVector<T> : global::System.Collections.Generic.IList<T>
	{
		// Forced skipping of method Windows.Foundation.Collections.IObservableVector<T>.VectorChanged.add
		// Forced skipping of method Windows.Foundation.Collections.IObservableVector<T>.VectorChanged.remove
		#if false || false || false || false
		 event global::Windows.Foundation.Collections.VectorChangedEventHandler<T> VectorChanged;
		#endif
	}
}
