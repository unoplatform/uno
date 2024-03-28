#pragma warning disable CS0618
using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Java.Lang;
using Uno.Disposables;
using Uno.UI.Controls;
using Uno.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
	{
		private static Paint _fallbackFillPaint;

		/// <summary>
		/// Returns the fallback solid color brush.
		/// </summary>
		/// <param name="destinationRect">Destination rect.</param>
		/// <returns></returns>
		private protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
		{
			paint.Color = FallbackColorWithOpacity;
		}

		internal IDisposable Subscribe(BindableView owner, Rect drawArea, Path maskingPath)
		{
			var state = new AcrylicState(owner, drawArea, maskingPath);

			var compositeDisposable = new CompositeDisposable(6);

			this.RegisterDisposablePropertyChangedCallback(
				AlwaysUseFallbackProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				FallbackColorProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				TintColorProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				TintOpacityProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				OpacityProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			Apply(state);

			Disposable.Create(() =>
			{
				state.FallbackDisposable.Disposable = null;
				state.BlurDisposable.Disposable = null;
			}).DisposeWith(compositeDisposable);

			return compositeDisposable;
		}

		private void Apply(AcrylicState state)
		{
			if (AlwaysUseFallback || !SupportsBlur())
			{
				state.BlurDisposable.Disposable = null;

				// Fall back to solid color
				_fallbackFillPaint ??= new();
				ApplyToFillPaint(Rect.Empty, _fallbackFillPaint);
				ExecuteWithNoRelayout(state.Owner, v => v.SetBackgroundDrawable(Brush.GetBackgroundDrawable(this, state.DrawArea, _fallbackFillPaint, state.MaskingPath, antiAlias: false)));

				if (state.FallbackDisposable.Disposable == null)
				{
					state.FallbackDisposable.Disposable = Disposable.Create(
						() => ExecuteWithNoRelayout(state.Owner, v => v.SetBackgroundDrawable(null)));
				}
			}
			else
			{
				state.FallbackDisposable.Disposable = null;

				ApplyAcrylicBlur(state);

				if (state.BlurDisposable.Disposable == null)
				{
					state.BlurDisposable.Disposable = Disposable.Create(
						() => RemoveAcrylicBlur(state));
				}
			}
		}

		private void ExecuteWithNoRelayout(BindableView view, Action<BindableView> action)
		{
			using (view.PreventRequestLayout())
			{
				action(view);
			}
		}

		/// <summary>
		/// Wraps the acrylic brush metadata for a single view.
		/// </summary>
		private class AcrylicState
		{
			public AcrylicState(BindableView owner, Rect drawArea, Path maskingPath)
			{
				Owner = owner;
				DrawArea = drawArea;
				MaskingPath = maskingPath;
			}

			public SerialDisposable FallbackDisposable { get; } = new SerialDisposable();

			public SerialDisposable BlurDisposable { get; } = new SerialDisposable();

			public GradientDrawable BackgroundDrawable { get; set; }

			public View BlurViewWrapper { get; set; }

			public RealtimeBlurView BlurView { get; set; }

			public BindableView Owner { get; }

			public Rect DrawArea { get; }

			public Path MaskingPath { get; }
		}
	}
}
#pragma warning restore CS6018
