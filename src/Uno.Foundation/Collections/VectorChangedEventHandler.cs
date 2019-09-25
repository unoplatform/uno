using Windows.Foundation.Metadata;

namespace Windows.Foundation.Collections
{
	public delegate void VectorChangedEventHandler<T>(IObservableVector<T> sender, IVectorChangedEventArgs @event);
	internal delegate void VectorChangedEventHandler(object sender, IVectorChangedEventArgs @event);
}
