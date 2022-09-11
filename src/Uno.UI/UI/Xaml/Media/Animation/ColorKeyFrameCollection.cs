using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class ColorKeyFrameCollection : DependencyObjectCollection<ColorKeyFrame>, IList<ColorKeyFrame>, IEnumerable<ColorKeyFrame>
	{
		public ColorKeyFrameCollection() : base(null, false)
		{
		}

		// Following members are for binary backward compatibility. They can be safely remove in an eventual "breaking" new version.
		public new uint Size => base.Size;

		public new IEnumerator<ColorKeyFrame> GetEnumerator() => base.GetEnumerator();

		public new void Add(ColorKeyFrame item) => base.Add(item);

		public new void Clear() => base.Clear();

		public new bool Contains(ColorKeyFrame item) => base.Contains(item);

		public new void CopyTo(ColorKeyFrame[] array, int arrayIndex) => base.CopyTo(array, arrayIndex);

		public new bool Remove(ColorKeyFrame item) => base.Remove(item);

		public new int Count
		{
			get => base.Count;
			set => throw new NotSupportedException();
		}

		public new bool IsReadOnly
		{
			get => base.IsReadOnly;
			set => throw new NotSupportedException();
		}

		public new int IndexOf(ColorKeyFrame item) => base.IndexOf(item);

		public new void Insert(int index, ColorKeyFrame item) => base.Insert(index, item);

		public new void RemoveAt(int index) => base.RemoveAt(index);

		public new ColorKeyFrame this[int index]
		{
			get => base[index];
			set => base[index] = value;
		}
	}
}
