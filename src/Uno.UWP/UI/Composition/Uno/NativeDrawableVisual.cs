#nullable enable

using System;
using System.Linq;
using Windows.UI;
using Windows.UI.Composition;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Java.Lang;

namespace Uno.UI.Composition
{
	internal class NativeDrawableVisual : Visual
	{
		private WeakReference<NativeDrawableVisual>? _weakThis;
		private Drawable? _drawable;
		private Callback? _callback;
		private AnimationCallback? _animationCallback;

		/// <summary>
		/// Create an adapter to use a <see cref="Drawable"/> as a <see cref="Visual"/>.
		/// </summary>
		/// <param name="context">The <see cref="UIContext"/> to which this visual is attached to.</param>
		public NativeDrawableVisual(UIContext context)
			: base(context.Compositor)
		{
			Kind = VisualKind.NativeDependent;
		}

		/// <summary>
		/// Create an adapter to use a <see cref="Drawable"/> as a <see cref="Visual"/>.
		/// </summary>
		/// <param name="drawable">The native drawable.</param>
		/// <param name="context">The <see cref="UIContext"/> to which this visual is attached to.</param>
		/// <param name="isStaticDrawable">Indicates that this drawable is used as an immutable object, so there is no needs to listen for updates.</param>
		public NativeDrawableVisual(Drawable drawable, UIContext context, bool isStaticDrawable = false)
			: this(context)
		{
			SetDrawable(drawable, isStaticDrawable);
		}

		/// <inheritdoc />
		private protected override void RenderDependent(Canvas canvas)
		{
			base.RenderDependent(canvas);

			_drawable?.SetBounds(0, 0, (int)Size.X, (int)Size.Y); // Offset is set on the RenderNode itself
			_drawable?.Draw(canvas);
		}

		/// <summary>
		/// Sets the <see cref="Drawable"/> to render.
		/// </summary>
		/// <param name="drawable">The native drawable.</param>
		/// <param name="isImmutableDrawable">Indicates that this drawable is used as an immutable object, so there is no needs to listen for updates.</param>
		public void SetDrawable(Drawable? drawable, bool isImmutableDrawable)
		{
			if (_drawable is { } previous)
			{
				Detach(previous);
				Kind = VisualKind.NativeDependent; // TODO
			}

			_drawable = drawable;

			if (drawable is { } @new)
			{
				Attach(@new, isImmutableDrawable);
			}
		}

		private void Attach(Drawable drawable, bool isStaticDrawable)
		{
			if (!isStaticDrawable)
			{
				_weakThis ??= new WeakReference<NativeDrawableVisual>(this);
				_callback ??= new Callback(_weakThis);

				drawable.Callback = _callback;
			}

			// Even if the drawable is static, if it can be animated
			if (drawable is IAnimatable2 animatable)
			{
				_weakThis ??= new WeakReference<NativeDrawableVisual>(this);
				_animationCallback ??= new AnimationCallback(_weakThis);

				animatable.RegisterAnimationCallback(_animationCallback);
			}
		}

		private void Detach(Drawable drawable)
		{
			if (_callback is { })
			{
				drawable.Callback = null;
			}

			if (drawable is IAnimatable2 animatable)
			{
				animatable.UnregisterAnimationCallback(_animationCallback!);
			}
		}

		/// <inheritdoc />
		private protected override void Dispose(bool isDispose)
		{
			base.Dispose(isDispose);

			if (_drawable is null) // Already disposed
			{
				return;
			}

			Detach(_drawable);
			_drawable = null;
		}

		private class Callback : Java.Lang.Object, Drawable.ICallback
		{
			private readonly WeakReference<NativeDrawableVisual> _owner;

			public Callback(WeakReference<NativeDrawableVisual> owner)
			{
				_owner = owner;
			}

			/// <inheritdoc />
			public void InvalidateDrawable(Drawable who)
			{
				if (_owner.TryGetTarget(out var owner))
				{
					owner.Invalidate(CompositionPropertyType.Dependent);
				}
			}

			/// <inheritdoc />
			public void ScheduleDrawable(Drawable who, IRunnable what, long when)
			{
			}

			/// <inheritdoc />
			public void UnscheduleDrawable(Drawable who, IRunnable what)
			{
			}
		}

		private class AnimationCallback : Animatable2AnimationCallback
		{
			private readonly WeakReference<NativeDrawableVisual> _owner;

			public AnimationCallback(WeakReference<NativeDrawableVisual> owner)
			{
				_owner = owner;
			}

			/// <inheritdoc />
			public override void OnAnimationStart(Drawable? drawable)
			{
				if (_owner.TryGetTarget(out var owner))
				{
					owner.Kind = VisualKind.NativeIndependent;
				}

				base.OnAnimationStart(drawable);
			}

			/// <inheritdoc />
			public override void OnAnimationEnd(Drawable? drawable)
			{
				if (_owner.TryGetTarget(out var owner))
				{
					owner.Kind = VisualKind.NativeDependent;
				}

				base.OnAnimationEnd(drawable);
			}
		}
	}
}
