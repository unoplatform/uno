using System.Collections.Generic;
using Windows.Foundation;
using Uno;
using CachedSize = Uno.CachedTuple<double, double>;
using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlockMeasureCache
	{
		class MeasureEntry
		{
			private Dictionary<CachedSize, MeasureSizeEntry> _sizes
				= new Dictionary<CachedSize, MeasureSizeEntry>();

			private LinkedList<CachedSize> _queue = new LinkedList<CachedSize>();

			public LinkedListNode<MeasureKey> ListNode { get; }

			public MeasureEntry(LinkedListNode<MeasureKey> node) => this.ListNode = node;

			public Size? FindMeasuredSize(MeasureKey key, Size availableSize)
			{
				if(_sizes.TryGetValue(CachedTuple.Create(availableSize.Width, availableSize.Height), out var sizeEntry))
				{
					return sizeEntry.MeasuredSize;
				}
				else
				{
					if(key.TextWrapping == TextWrapping.NoWrap)
					{
						// No wrap, assume any measured width below the asked available size
						// is valid, if the available size is greater.
						foreach(var keySize in _sizes)
						{
							if(keySize.Key.Item1 >= availableSize.Width &&
								keySize.Value.MeasuredSize.Width <= availableSize.Width)
							{
								MoveToLast(keySize.Key, keySize.Value);
								return keySize.Value.MeasuredSize;
							}
						}
					}
					else
					{
						foreach (var keySize in _sizes)
						{
							// If text wraps and the available width is the same, any height below the
							// available size is valid.
							if (
								keySize.Key.Item1 == availableSize.Width
								&& keySize.Value.MeasuredSize.Height <= availableSize.Height
							)
							{
								MoveToLast(keySize.Key, keySize.Value);
								return keySize.Value.MeasuredSize;
							}
						}
					}
				}
				return null;
			}

			private void MoveToLast(CachedSize key, MeasureSizeEntry value)
			{
				if(_queue.Count != 0 && _queue.Last.Value != key)
				{
					_queue.Remove(value.ListNode);
					_queue.AddLast(value.ListNode);
				}
			}

			internal void CacheMeasure(Size desiredSize, Size measuredSize)
			{
				Scavenge();

				var key = CachedTuple.Create(desiredSize.Width, desiredSize.Height);
				var node = _queue.AddLast(key);
				_sizes[key] = new MeasureSizeEntry(measuredSize, node);
			}

			private void Scavenge()
			{
				if (_queue.Count >= MaxMeasureSizeKeyEntries)
				{
					_sizes.Remove(_queue.First.Value);
					_queue.RemoveFirst();
				}
			}
		}

	}
}
