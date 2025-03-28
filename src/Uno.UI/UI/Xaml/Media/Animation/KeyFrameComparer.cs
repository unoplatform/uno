#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.UI.Xaml.Media.Animation
{
	internal class KeyFrameComparer : IComparer<IKeyFrame>
	{
		public static KeyFrameComparer Instance { get; } = new KeyFrameComparer();

		private KeyFrameComparer()
		{
		}

		/// <inheritdoc />
		public int Compare(IKeyFrame? x, IKeyFrame? y)
			=> CompareCore(x, y);

		public static int CompareCore(IKeyFrame? x, IKeyFrame? y)
		{
			// We consider 'null' as always lower than other items
			if (x is null)
			{
				return y is null ? 0 : int.MinValue;
			}
			else if (y is null)
			{
				return int.MaxValue;
			}

			return ((IComparable<KeyTime>)x.KeyTime).CompareTo(y.KeyTime);
		}
	}

	internal class KeyFrameComparer<TValue> : IComparer<IKeyFrame<TValue>>
	{
		public static KeyFrameComparer<TValue> Instance { get; } = new KeyFrameComparer<TValue>();

		private KeyFrameComparer()
		{
		}

		/// <inheritdoc />
		public int Compare(IKeyFrame<TValue>? x, IKeyFrame<TValue>? y)
			=> KeyFrameComparer.CompareCore(x, y);
	}
}
