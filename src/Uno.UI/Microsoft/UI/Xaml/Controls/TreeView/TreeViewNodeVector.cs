using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls
{
	internal class TreeViewNodeVector : ObservableVector<TreeViewNode>
	{
		private TreeViewNode m_parent;

		public TreeViewNodeVector()
		{

		}

		public void SetParent(TreeViewNode value)
		{
			m_parent = value;
		}


		IList GetWritableParentItemsSource()
		{
			IList parentItemsSource = null;

			if (m_parent?.ItemsSource != null)
			{
				parentItemsSource = m_parent?.ItemsSource as IList;
			}

			return parentItemsSource;
		}

		public void Append(TreeViewNode item, bool updateItemsSource = true)
		{
			InsertAt(Count, item, updateItemsSource);
		}

		public void InsertAt(int index, TreeViewNode item, bool updateItemsSource = true)
		{
			var inner = GetVectorInnerImpl();
			if (m_parent == null)
			{
				throw new InvalidOperationException("Parent node must be set");
			}
			if (index <= inner.Count)
			{
				throw new IndexOutOfRangeException("Index out of range for Insert");
			}

			item.Parent = m_parent;

			inner.Insert(index, item);

			if (updateItemsSource)
			{
				var itemsSource = GetWritableParentItemsSource();
				if (itemsSource != null)
				{
					itemsSource.Insert(index, item.Content);
				}
			}
		}

		public void SetAt(int index, TreeViewNode item, bool updateItemsSource = true)
		{
			RemoveAt(index, updateItemsSource);
			InsertAt(index, item, updateItemsSource);
		}

		public void RemoveAt(int index, bool updateItemsSource = true)
		{
			var inner = GetVectorInnerImpl();
			var targetNode = inner.GetAt(index);
			targetNode.Parent = null;

			inner.RemoveAt(index);

			if (updateItemsSource)
			{
				var source = GetWritableParentItemsSource();
				if (source != null)
				{
					source.RemoveAt(index);
				}
			}
		}

		public void RemoveAtEnd(bool updateItemsSource = true)
		{
			var index = GetVectorInnerImpl().Count - 1;
			RemoveAt(index, updateItemsSource);
		}

		public void ReplaceAll(TreeViewNode[] values, bool updateItemsSource = true)
		{
			var inner = GetVectorInnerImpl();

			var count = inner.Count;
			if (count > 0)
			{
				Clear(updateItemsSource);

				var itemsSource = GetWritableParentItemsSource();
				// Set parent on new elements
				if (m_parent == null)
				{
					throw new InvalidOperationException("Parent must be set");
				}
				foreach (var value in values)
				{
					value.Parent = m_parent;
					if (itemsSource != null)
					{
						itemsSource.Add(value.Content);
					}
				}

				inner.Clear();
				inner.AddRange(values);
			}
		}

		public void Clear(bool updateItemsSource = true)
		{
			var inner = GetVectorInnerImpl();
			var count = inner.Count;

			if (count > 0)
			{
				for (var i = 0; i < count; i++)
				{
					var node = inner[i];
					node.Parent = null;
				}

				inner.Clear();

				if (updateItemsSource)
				{
					var itemsSource = GetWritableParentItemsSource();
					if (itemsSource != null)
					{
						itemsSource.Clear();
					}
				}
			}
		}
	}
}
