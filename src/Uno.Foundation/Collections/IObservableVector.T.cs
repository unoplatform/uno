using System.Collections.Generic;
using Windows.Foundation.Metadata;

namespace Windows.Foundation.Collections
{
	public partial interface IObservableVector<T> : IList<T>
	{
		/// <summary>
		/// Occurs when the vector changes.
		/// </summary>
		event VectorChangedEventHandler<T> VectorChanged;
	}
}
