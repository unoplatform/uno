using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Tests.Helpers
{
	public class GroupedObservableCollection<TKey> : ObservableCollection<object>, IGrouping<TKey, object>
	{
		public TKey Key { get; }

		public GroupedObservableCollection(TKey key) : base()
		{
			Key = key;
		}

		public GroupedObservableCollection(IGrouping<TKey, object> collection) : base(collection)
		{
			Key = collection.Key;
		}
	}

}
