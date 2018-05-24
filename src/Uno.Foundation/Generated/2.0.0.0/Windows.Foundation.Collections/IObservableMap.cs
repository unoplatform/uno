#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Collections
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IObservableMap<K, V> : global::System.Collections.Generic.IDictionary<K, V>
	{
		// Forced skipping of method Windows.Foundation.Collections.IObservableMap<K, V>.MapChanged.add
		// Forced skipping of method Windows.Foundation.Collections.IObservableMap<K, V>.MapChanged.remove
		#if false || false || false || false
		 event global::Windows.Foundation.Collections.MapChangedEventHandler<K, V> MapChanged;
		#endif
	}
}
