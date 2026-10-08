#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Text
{
	internal readonly struct TextElementBoundaryView
	{
		private readonly int[] _boundaries;

		internal TextElementBoundaryView(int[] boundaries, int count)
		{
			_boundaries = boundaries;
			Count = count;
		}

		internal int Count { get; }

		internal int TextLength => _boundaries[Count - 1];

		internal int[] Boundaries => _boundaries;

		internal int this[int index] => _boundaries[index];

		internal int BinarySearch(int value)
			=> Array.BinarySearch(_boundaries, 0, Count, value);

		internal int GetStart(int position)
		{
			if (position <= 0 || position >= TextLength)
			{
				return position;
			}

			var index = BinarySearch(position);
			return index >= 0 ? position : _boundaries[Math.Max(0, ~index - 1)];
		}

		internal int GetEnd(int position)
		{
			if (position <= 0 || position >= TextLength)
			{
				return position;
			}

			var index = BinarySearch(position);
			return index >= 0 ? position : _boundaries[~index];
		}
	}

	internal sealed class TextElementBoundaryCache
	{
		private int[] _boundaries = new int[16];
		private int _count;
		private long _version = -1;

		internal int RebuildCount { get; private set; }

		internal int StorageBytes => checked(_count * sizeof(int));

		internal TextElementBoundaryView Get(string text, long version)
		{
			if (_version != version)
			{
				Rebuild(text);
				_version = version;
				RebuildCount++;
			}

			return new TextElementBoundaryView(_boundaries, _count);
		}

		private void Rebuild(string text)
		{
			_count = 0;
			var enumerator = StringInfo.GetTextElementEnumerator(text);
			while (enumerator.MoveNext())
			{
				Add(enumerator.ElementIndex);
			}

			if (_count == 0 || _boundaries[_count - 1] != text.Length)
			{
				Add(text.Length);
			}
		}

		private void Add(int boundary)
		{
			if (_count == _boundaries.Length)
			{
				Array.Resize(ref _boundaries, checked(_boundaries.Length * 2));
			}

			_boundaries[_count++] = boundary;
		}
	}
}
