using System;
using System.Numerics;
using Windows.Foundation;


namespace Microsoft.UI.Xaml.Media
{
	public partial class GeneralTransform : DependencyObject, IMultiParentShareableDependencyObject
	{
		protected GeneralTransform() { }

		public GeneralTransform Inverse => InverseCore;

		protected virtual GeneralTransform InverseCore { get; }

		public Point TransformPoint(Point point)
		{
			TryTransform(point, out var transformed);
			return transformed;
		}

		public bool TryTransform(Point inPoint, out Point outPoint)
			=> TryTransformCore(inPoint, out outPoint);

		protected virtual bool TryTransformCore(Point inPoint, out Point outPoint)
		{
			outPoint = inPoint;
			return false;
		}

		public Rect TransformBounds(Rect rect)
			=> TransformBoundsCore(rect);

		protected virtual Rect TransformBoundsCore(Rect rect)
			=> rect;

		/// <summary>
		/// Transforms a point using the inverse of this transform.
		/// </summary>
		/// <param name="inPoint">The point to transform.</param>
		/// <param name="outPoint">The transformed point, or <paramref name="inPoint"/> if this transform is not invertible.</param>
		/// <returns>True if the inverse transform could be applied, false if this transform is not invertible.</returns>
		/// <remarks>
		/// Derived types are expected to invert their underlying matrix directly so callers that only need an immediate
		/// transformation don't have to allocate an intermediate <see cref="Inverse"/> transform.
		/// </remarks>
		internal virtual bool TryTransformInverse(Point inPoint, out Point outPoint)
		{
			if (InverseCore is { } inverse)
			{
				outPoint = inverse.TransformPoint(inPoint);
				return true;
			}

			outPoint = inPoint;
			return false;
		}

		/// <summary>
		/// Transforms the bounding box of a rectangle using the inverse of this transform.
		/// </summary>
		/// <param name="rect">The rectangle to transform.</param>
		/// <param name="outRect">The transformed bounds, or <paramref name="rect"/> if this transform is not invertible.</param>
		/// <returns>True if the inverse transform could be applied, false if this transform is not invertible.</returns>
		/// <remarks>
		/// Derived types are expected to invert their underlying matrix directly so callers that only need an immediate
		/// transformation don't have to allocate an intermediate <see cref="Inverse"/> transform.
		/// </remarks>
		internal virtual bool TryTransformBoundsInverse(Rect rect, out Rect outRect)
		{
			if (InverseCore is { } inverse)
			{
				outRect = inverse.TransformBounds(rect);
				return true;
			}

			outRect = rect;
			return false;
		}
	}
}
