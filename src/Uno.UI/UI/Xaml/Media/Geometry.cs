using System.Drawing;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using Uno.Media;
using Windows.Foundation;

using Rect = Windows.Foundation.Rect;
using SkiaSharp;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media
{
	[TypeConverter(typeof(GeometryConverter))]
	public partial class Geometry : DependencyObject, IDisposable
	{
		internal Geometry()
		{
			InitializeBinder();
		}

		internal event Action GeometryChanged;

		private protected void RaiseGeometryChanged()
			=> GeometryChanged?.Invoke();

		public static implicit operator Geometry(string data)
		{
			return Parsers.ParseGeometry(data);
		}

		public Rect Bounds => ComputeBounds();

		public static Geometry Empty => new PathGeometry();

		public static double StandardFlatteningTolerance => 0.25;

		private protected virtual Rect ComputeBounds()
		{
			throw new NotImplementedException($"Bounds property is not implemented on {GetType().Name}.");
		}

		#region Transform

		public Transform Transform
		{
			get => (Transform)this.GetValue(TransformProperty);
			set => this.SetValue(TransformProperty, value);
		}

		public static DependencyProperty TransformProperty { get; } =
			DependencyProperty.Register(
				"Transform",
				typeof(Transform),
				typeof(Geometry),
				new FrameworkPropertyMetadata(default(Transform), propertyChangedCallback: (s, args) => ((Geometry)s).OnTransformChanged(args))
			);

		private void OnTransformChanged(DependencyPropertyChangedEventArgs args)
		{
			RaiseGeometryChanged();

			if (args.OldValue is Transform oldValue)
			{
				oldValue.Changed -= OnTransformSubChanged;
			}

			if (args.NewValue is Transform newValue)
			{
				newValue.Changed += OnTransformSubChanged;
			}
		}

		private void OnTransformSubChanged(object sender, EventArgs e)
			=> RaiseGeometryChanged();

		#endregion

		public virtual void Dispose() { throw new InvalidOperationException(); }

		// TODO: Can we mark Geometry and GetSKPath method as abstract?
		// While this will diverge from UWP, it doesn't seem to matter whether it's abstract or not because
		// this class doesn't have public constructors in UWP, which makes it not-inheritable either way.
		internal virtual SKPath GetSKPath() => throw new NotSupportedException($"Geometry {this} is not supported");

		/// <remarks>
		/// Note: Try not to depend on this. See the note in <see cref="CompositionSpriteShape.NegativeFillGeometry"/>
		/// </remarks>
		internal virtual SKPath GetFilledSKPath() => null;

		/// <summary>
		/// Returns the SKPath with the <see cref="Transform"/> applied, if any.
		/// </summary>
		internal SKPath GetTransformedSKPath()
		{
			var path = GetSKPath();
			return ApplyTransformToPath(path);
		}

		/// <summary>
		/// Returns the filled SKPath with the <see cref="Transform"/> applied, if any.
		/// </summary>
		internal SKPath GetTransformedFilledSKPath()
		{
			var path = GetFilledSKPath();
			return ApplyTransformToPath(path);
		}

		private SKPath ApplyTransformToPath(SKPath path)
		{
			if (path is null)
			{
				return null;
			}

			if (Transform is { MatrixCore: var matrix } && !matrix.IsIdentity)
			{
				var skMatrix = matrix.ToSKMatrix();
				var transformed = new SKPath();
				path.Transform(skMatrix, transformed);
				return transformed;
			}

			return path;
		}

		internal virtual SkiaGeometrySource2D GetGeometrySource2D() => new SkiaGeometrySource2D(GetTransformedSKPath());
	}
}
