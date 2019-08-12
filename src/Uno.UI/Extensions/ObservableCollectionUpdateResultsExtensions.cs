using System.Linq;

namespace Uno.Extensions
{
	public static class ObservableCollectionUpdateResultsExtensions
	{
		public static bool HasChanged<T>(this ObservableCollectionUpdateResults<T> observableCollection)
		{
			return observableCollection.Moved.Any()
				|| observableCollection.Added.Any()
				|| observableCollection.Removed.Any();
		}
	}
}
