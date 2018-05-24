using Windows.Foundation.Metadata;

namespace Windows.Foundation.Collections
{
	public delegate void VectorChangedEventHandler<T>(IObservableVector<T> sender, IVectorChangedEventArgs @event);
}