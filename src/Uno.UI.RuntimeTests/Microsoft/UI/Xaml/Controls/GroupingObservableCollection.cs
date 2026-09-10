using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	internal class GroupingObservableCollection<TKey, TElement> : ObservableCollection<TElement>, IGrouping<TKey, TElement>
	{
		public TKey Key { get; }

		public GroupingObservableCollection(TKey key) : base()
		{
			Key = key;
		}

		public GroupingObservableCollection(TKey key, IEnumerable<TElement> collection) : base(collection)
		{
			Key = key;
		}
	}
}
