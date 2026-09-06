using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

#if UNO_REFERENCE_API
using _View = Microsoft.UI.Xaml.UIElement;
using Uno.Extensions;
#else
using _View = System.Object;
#endif

namespace Uno.UI.Media
{
	/// <summary>
	/// Adapts an UWP <see cref="Transform"/> used as <see cref="UIElement.RenderTransform"/> to a native transformation
	/// </summary>
	public sealed partial class NativeRenderTransformAdapter : IDisposable
	{
		public NativeRenderTransformAdapter(_View owner, Transform transform, Point origin)
		{
			Owner = owner;
			Transform = transform;
			CurrentOrigin = origin;
			CurrentSize = owner is IFrameworkElement fwElt
				? new Size(fwElt.ActualWidth, fwElt.ActualHeight)
				: new Size(0, 0);

			// Partial constructor
			Initialized();

			if (Transform is not null)
			{
				Transform.Changed += UpdateOnTransformPropertyChanged;
			}
		}

		internal NativeRenderTransformAdapter(_View owner, Transform transform, Point origin, Matrix3x2 flowDirectionTransform)
			: this(owner, transform, origin)
		{
			FlowDirectionTransform = flowDirectionTransform;
		}

		partial void Initialized();

		/// <summary>
		/// The view on which this render transform has been declared
		/// </summary>
		public _View Owner { get; }

		/// <summary>
		/// The render transform
		/// </summary>
		public Transform Transform { get; }

		public Matrix3x2 FlowDirectionTransform { get; private set; } = Matrix3x2.Identity;

		/// <summary>
		/// The current relative origin of this render transform.
		/// </summary>
		public Point CurrentOrigin { get; private set; }

		/// <summary>
		/// The current size of this render transform
		/// </summary>
		public Size CurrentSize { get; private set; }

		public void UpdateOrigin(Point origin)
		{
			CurrentOrigin = origin;
			Update(isOriginChanged: true);
		}

		public void UpdateSize(Size size)
		{
			CurrentSize = size;
			Update(isSizeChanged: true);
		}

		public void UpdateFlowDirectionTransform()
		{
			Update();
		}

		private void UpdateOnTransformPropertyChanged(object snd, EventArgs args)
			=> Update();

		private void Update(bool isSizeChanged = false, bool isOriginChanged = false)
		{
			Apply(isSizeChanged, isOriginChanged);
		}

		/// <summary>
		/// Natively applies this current render transform to its <see cref="Owner"/>
		/// </summary>
		partial void Apply(bool isSizeChanged, bool isOriginChanged);

		/// <summary>
		/// Natively cleanup this current render transform from its <see cref="Owner"/> before being removed.
		/// </summary>
		partial void Cleanup();

		/// <inheritdoc />
		public void Dispose()
		{
			Transform.Changed -= UpdateOnTransformPropertyChanged;
			Cleanup();
		}

		partial void Initialized()
		{
			// Apply the transform as soon as its been declared
			Update();
		}

		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
			FlowDirectionTransform = Owner.GetFlowDirectionTransform();

			// Get base 2D transform (RenderTransform + FlowDirection)
			Matrix3x2 transform2D;
			if (Transform is null)
			{
				transform2D = FlowDirectionTransform;
			}
			else
			{
				transform2D = Transform.ToMatrix(CurrentOrigin, CurrentSize) * FlowDirectionTransform;
			}

			// Convert to 4x4 matrix
			var finalMatrix = new Matrix4x4(transform2D);

			// Apply projection if set
			if (Owner is UIElement element && element.GetProjection() is Projection projection)
			{
				var projectionMatrix = projection.GetProjectionMatrix(CurrentSize);
				// Projection is applied after RenderTransform
				finalMatrix = finalMatrix * projectionMatrix;
			}

			Owner.Visual.TransformMatrix = finalMatrix;
		}

		partial void Cleanup()
		{
			FlowDirectionTransform = Matrix3x2.Identity;
			Owner.Visual.TransformMatrix = Matrix4x4.Identity;
		}
	}
}
