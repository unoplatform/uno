#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Tests.Helpers
{
	internal class ObservableCollectionEx<TType> : System.Collections.ObjectModel.ObservableCollection<TType>
	{
		private int _batchUpdateCount;

		public IDisposable BatchUpdate()
		{
			++_batchUpdateCount;

			return Uno.Disposables.Disposable.Create(Release);

			void Release()
			{
				if (--_batchUpdateCount <= 0)
				{
					OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
				}
			}
		}

		protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (_batchUpdateCount > 0)
			{
				return;
			}

			base.OnCollectionChanged(e);
		}

		public void Append(TType item) => Add(item);

		public TType GetAt(int index) => this[index];
	}
}
