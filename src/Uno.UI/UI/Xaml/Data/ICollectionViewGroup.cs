
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Data
{
	public partial interface ICollectionViewGroup
	{
		object Group { get; }
		IObservableVector<object> GroupItems { get; }
	}
}
