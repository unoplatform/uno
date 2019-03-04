using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Media
{
	public partial class GeneralTransform : DependencyObject
	{
		protected GeneralTransform() { }

		public GeneralTransform Inverse { get; }

		protected virtual GeneralTransform InverseCore { get; }

		public Rect TransformBounds(Rect rect) => TransformBoundsCore(rect);

		public Point TransformPoint(Point point)
		{
			if (TryTransform(point, out var output))
			{
				return output;
			}

			throw new InvalidOperationException();
		}

		public bool TryTransform(Point inPoint, out Point outPoint)
		{
			return TryTransformCore(inPoint, out outPoint);
		}

		protected virtual Rect TransformBoundsCore(Rect rect)
		{
			// This should be implemented by derived transforms
			throw new NotSupportedException();
		}

		protected virtual bool TryTransformCore(Point inPoint, out Point outPoint)
		{
			// This should be implemented by derived transforms
			throw new NotSupportedException();
		}
	}
}