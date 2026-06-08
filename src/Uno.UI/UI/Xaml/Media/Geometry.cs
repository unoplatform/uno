using System.Drawing;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using Uno.Media;
using Windows.Foundation;

using Rect = Windows.Foundation.Rect;

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
	}
}
