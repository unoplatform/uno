using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#elif UNO_REFERENCE_API
using _View = Windows.UI.Xaml.UIElement;
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

#if __ANDROID__ || __IOS__ || __MACOS__
			// For backward compatibility we set the "View" property on the transform
			// This is used only by animations
			if (transform is not null)
			{
				transform.View = owner;
			}
#endif

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
	}
}
