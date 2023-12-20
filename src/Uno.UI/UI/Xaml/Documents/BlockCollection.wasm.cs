using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents.TextFormatting;

namespace Windows.UI.Xaml.Documents;

partial class BlockCollection : IList<Block>, IEnumerable<Block>
{
	private readonly UIElementCollection _collection;

	internal BlockCollection(UIElement containerElement)
	{
		_collection = new UIElementCollection(containerElement);
		_collection.CollectionChanged += OnCollectionChanged;
	}

	private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		switch (_collection.Owner)
		{
			case ISegmentsElement segmentsElement:
				segmentsElement.InvalidateSegments();
				break;
			case Block block:
				block.InvalidateSegments();
				break;
			default:
				break;
		}
	}

	/// <inheritdoc />
	public IEnumerator<Block> GetEnumerator() => _collection.OfType<Block>().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <inheritdoc />
	public void Add(Block item) => _collection.Add(item);

	/// <inheritdoc />
	public void Clear() => _collection.Clear();

	/// <inheritdoc />
	public bool Contains(Block item) => _collection.Contains(item);

	/// <inheritdoc />
	public void CopyTo(Block[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public bool Remove(Block item) => _collection.Remove(item);

	/// <inheritdoc />
	public int Count => _collection.Count;

	/// <inheritdoc />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public int IndexOf(Block item) => _collection.IndexOf(item);

	/// <inheritdoc />
	public void Insert(int index, Block item) => _collection.Insert(index, item);

	/// <inheritdoc />
	public void RemoveAt(int index) => _collection.RemoveAt(index);

	/// <inheritdoc />
	public Block this[int index]
	{
		get => (Block)_collection[index];
		set => _collection[index] = value;
	}
}
