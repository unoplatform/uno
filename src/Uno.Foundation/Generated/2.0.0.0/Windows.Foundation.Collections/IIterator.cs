#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Collections
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IIterator<T> 
	{
		#if false
		T Current
		{
			get;
		}
		#endif
		#if false
		bool HasCurrent
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Foundation.Collections.IIterator<T>.Current.get
		// Forced skipping of method Windows.Foundation.Collections.IIterator<T>.HasCurrent.get
		#if false
		bool MoveNext();
		#endif
		#if false
		uint GetMany( T[] items);
		#endif
	}
}
