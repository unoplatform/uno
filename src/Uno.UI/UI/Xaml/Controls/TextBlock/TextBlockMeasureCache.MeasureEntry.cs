#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno;
using CachedSize = Uno.CachedTuple<double, double>;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class TextBlockMeasureCache
	{
		/// <summary>
		/// A set of TextBlock measures for a set of text characteristics
		/// </summary>
		private class MeasureEntry
		{
			private readonly Dictionary<CachedSize, MeasureSizeEntry> _sizes
				= new Dictionary<CachedSize, MeasureSizeEntry>();

			private readonly LinkedList<CachedSize> _queue = new LinkedList<CachedSize>();

			public LinkedListNode<MeasureKey> ListNode { get; }

			public MeasureEntry(LinkedListNode<MeasureKey> node) => this.ListNode = node;

			public Size? FindMeasuredSize(MeasureKey key, Size availableSize)
			{
				var currentAvailableWidth = availableSize.Width;
				var currentAvailableHeight = availableSize.Height;

				// Exact match
				if (_sizes.TryGetValue(CachedTuple.Create(currentAvailableWidth, currentAvailableHeight), out var sizeEntry))
				{
					return sizeEntry.MeasuredSize;
				}

				var isWrapping = key.IsWrapping;
				var isClipping = key.IsClipping;

				foreach (var kvp in _sizes)
				{
					var size = kvp.Key;
					var measureSizeEntry = kvp.Value;
					var measurementCachedWidth = measureSizeEntry.MeasuredSize.Width;
					var measurementCachedHeight = measureSizeEntry.MeasuredSize.Height;
					var measurementAvailableWidth = size.Item1;
					var measurementAvailableHeight = size.Item2;

					if (isWrapping || isClipping)
					{
						if (measurementCachedWidth <= currentAvailableWidth
							&& currentAvailableWidth <= measurementAvailableWidth)
						{
							// Ok we can reuse it
						}
						else
						{
							continue; // Check for another cached measurement
						}
					}
					else
					{
						// Non-wrapping text

						if (double.IsInfinity(measurementAvailableWidth))
						{
							// Previous measurement was unconstrained
							// horizontally: we can definitely reuse it.
						}
						else
						{
							if (Math.Abs(measurementCachedWidth - measurementAvailableWidth) <= 0.5d)
							{
								// This measure was constrained, we can reuse only if the width is the same.

								if (Math.Abs(measurementCachedWidth - currentAvailableWidth) <= 0.5d)
								{
									// Yep, that's good
								}

								else
								{
									continue; // Check for another cached measurement
								}
							}
						}
					}

					// We need to make sure the height is ok
					if (double.IsInfinity(measurementAvailableHeight))
					{
						// Previous measurement was unconstrained
						// vertically: we can definitely reuse it.
					}
					else
					{
						// A max-height was specified in the cached measurement:
						// We must check if we can reuse it.
						if (Math.Abs(measurementCachedHeight - measurementAvailableHeight) <= 0.5d)
						{
							// This measure was constrained, we can reuse only if the available height
							// is same or higher than current available height
							if (measurementCachedHeight >= currentAvailableHeight)
							{
								// Yep, that's good
							}

							else
							{
								continue; // Check for another cached measurement
							}
						}
					}

					// Got it, this cached measurement fits
					MoveToLast(size, measureSizeEntry);
					return measureSizeEntry.MeasuredSize;
				}

				return null; // No valid cache entry found
			}

			private void MoveToLast(CachedSize key, MeasureSizeEntry value)
			{
				if (_queue.Count == 0
					|| (_queue.Last is not null && Equals(_queue.Last.Value, key)))
				{
					return;
				}

				_queue.Remove(value.ListNode);
				_queue.AddLast(value.ListNode);
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
				if (_queue.Count < MaxMeasureSizeKeyEntries || _queue.First is null)
				{
					return;
				}

				_sizes.Remove(_queue.First.Value);
				_queue.RemoveFirst();
			}
		}
	}
}
