using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
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

		private IList GetWritableParentItemsSource()
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

		public override void Add(TreeViewNode item) => Append(item);

		public void InsertAt(int index, TreeViewNode item, bool updateItemsSource = true)
		{			
			if (m_parent == null)
			{
				throw new InvalidOperationException("Parent node must be set");
			}
			if (index > base.Count)
			{
				throw new IndexOutOfRangeException("Index out of range for Insert");
			}

			item.Parent = m_parent;

			base.Insert(index, item);

			if (updateItemsSource)
			{
				var itemsSource = GetWritableParentItemsSource();
				if (itemsSource != null)
				{
					itemsSource.Insert(index, item.Content);
				}
			}
		}

		public override void Insert(int index, TreeViewNode item) => InsertAt(index, item);

		public void SetAt(int index, TreeViewNode item, bool updateItemsSource = true)
		{
			RemoveAt(index, updateItemsSource);
			InsertAt(index, item, updateItemsSource);
		}

		public override TreeViewNode this[int index]
		{
			get => base[index];
			set => SetAt(index, value);
		}

		public void RemoveAt(int index, bool updateItemsSource = true)
		{
			var targetNode = this[index];
			targetNode.Parent = null;

			base.RemoveAt(index);

			if (updateItemsSource)
			{
				var source = GetWritableParentItemsSource();
				if (source != null)
				{
					source.RemoveAt(index);
				}
			}
		}

		public override void RemoveAt(int index) => RemoveAt(index, true);

		public void RemoveAtEnd(bool updateItemsSource = true)
		{
			var index = Count - 1;
			RemoveAt(index, updateItemsSource);
		}

		public void ReplaceAll(TreeViewNode[] values, bool updateItemsSource = true)
		{
			var count = Count;
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

				base.Clear();
				foreach (var value in values)
				{
					base.Add(value);
				}
			}
		}

		public void Clear(bool updateItemsSource = true)
		{			
			var count = Count;

			if (count > 0)
			{
				for (var i = 0; i < count; i++)
				{
					var node = this[i];
					node.Parent = null;
				}

				base.Clear();

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

		public override void Clear() => Clear(true);
	}
}
