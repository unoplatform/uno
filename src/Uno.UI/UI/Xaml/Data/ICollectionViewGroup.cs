
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Data
{
	public partial interface ICollectionViewGroup
	{
		object Group { get; }
		IObservableVector<object> GroupItems { get; }
	}
}
