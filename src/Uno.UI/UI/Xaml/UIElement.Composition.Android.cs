#nullable enable

using System;
using Windows.UI.Composition;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Uno.UI.Composition;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml
{
	public partial class UIElement
	{
		private NativeDrawableVisual? _background;
		private NativeDrawableVisual? _overlay;
		private Drawable? _nativeOverlay;

		partial void InitializeCompositionPartial()
		{
			if (Uno.CompositionConfiguration.UseVisual)
			{
				// This is required for the Draw/OnDraw to be invoked
				SetWillNotDraw(willNotDraw: false);
			}
		}

		/// <inheritdoc />
		public override void Draw(Canvas? canvas)
		{
			// Note: If possible we prefer to override the Draw (instead of the recommended OnDraw),
			//		 as the base (native) implementation is rendering Background and Foreground layers,
			//		 which are hopefully also rendered by Visual.

			if (Uno.CompositionConfiguration.UseVisualForLayers)
			{
				UIContext.Compositor.Render(Visual, canvas!);
			}
			else
			{
				base.Draw(canvas);
			}
		}

		/// <inheritdoc />
		protected override void OnDraw(Canvas? canvas)
		{
			// Note: We reach this method only if the UseVisualForLayers was set to false
			//		 (or if the view as requested draw callback but does not use Visual at all)

			if (Uno.CompositionConfiguration.UseVisual)
			{
				UIContext.Compositor.Render(Visual, canvas!);
			}
			else
			{
				base.OnDraw(canvas);
			}
		}

		/// <inheritdoc />
		protected override void DispatchDraw(Canvas? canvas)
		{
			if (Uno.CompositionConfiguration.UseVisual)
			{
				// If we have a composition tree, we should not dispatch draw to children
				return;
			}

			base.DispatchDraw(canvas);
		}

		/// <summary>
		/// Set the native <see cref="Drawable"/> to use as the background layer for this UIElement.
		/// </summary>
		/// <param name="background">The drawable.</param>
		/// <param name="isImmutable">Indicates that this drawable is used as an immutable object, so there is no needs to listen for updates.</param>
#pragma warning disable 0618, 0672 // Used for compatibility with SetBackgroundDrawable and previous API Levels
		public void SetBackground(Drawable? background, bool isImmutable = false)
		{
			if (Uno.CompositionConfiguration.UseVisualForLayers)
			{
				if (_background is null)
				{
					_background = new NativeDrawableVisual(UIContext);
					Visual.Children.InsertAtBottom(_background);
				}

				_background.SetDrawable(background, isImmutable);
			}
			else
			{
				base.SetBackgroundDrawable(background);
			}
		}

		/// <inheritdoc />
		public override void SetBackgroundDrawable(Drawable? background)
			=> this.SetBackground(background);
#pragma warning restore 0618, 0672

		/// <summary>
		/// Set the native <see cref="Drawable"/> to use as the overlay layer for this UIElement.
		/// </summary>
		/// <param name="overlay">The drawable.</param>
		/// <param name="isStaticDrawable">Indicates that this drawable is used as an immutable object, so there is no needs to listen for updates.</param>
		public void SetOverlay(Drawable? overlay, bool isStaticDrawable = false)
		{
			if (Uno.CompositionConfiguration.UseVisualForLayers)
			{
				if (_overlay is null)
				{
					_overlay = new NativeDrawableVisual(UIContext);
					Visual.Children.InsertAtTop(_overlay);
				}

				_overlay.SetDrawable(overlay, isStaticDrawable);
			}
			else
			{
				if (_nativeOverlay is { } previous)
				{
					base.Overlay!.Remove(previous);
				}

				_nativeOverlay = overlay;

				if (overlay is { } @new)
				{
					base.Overlay!.Add(@new);
				}
			}
		}

		private protected override void OnChildViewAddedInternal(int index, View view)
		{
			if (Uno.CompositionConfiguration.UseVisual)
			{
				var visual = view is UIElement elt
					? elt.Visual as Visual
					: new NativeViewVisual(view, UIContext);

				if (_background is { })
				{
					index += 1;
				}

				Visual.Children.InsertAt(index, visual);
			}
		}

		private protected override void OnChildViewMovedInternal(int oldIndex, int newIndex, View view)
		{
			if (Uno.CompositionConfiguration.UseVisual)
			{
				if (_background is { })
				{
					oldIndex += 1;
					newIndex += 1;
				}

				Visual.Children.Move(oldIndex, newIndex);
			}
		}

		private protected override void OnChildViewRemovedInternal(int index, View view)
		{
			if (Uno.CompositionConfiguration.UseVisual)
			{
				if (_background is { })
				{
					index += 1;
				}

				Visual.Children.RemoveAt(index);
			}
		}
	}
}
