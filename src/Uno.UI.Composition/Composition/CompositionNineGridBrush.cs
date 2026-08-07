#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionNineGridBrush : CompositionBrush
	{
		private float _bottomInset;
		private float _bottomInsetScale = 1.0f;

		private float _leftInset;
		private float _leftInsetScale = 1.0f;

		private float _rightInset;
		private float _rightInsetScale = 1.0f;

		private float _topInset;
		private float _topInsetScale = 1.0f;

		private bool _isCenterHollow;
		private CompositionBrush? _source;

		internal CompositionNineGridBrush(Compositor compositor) : base(compositor)
		{

		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			// Call base implementation - Visual calls Compositor.InvalidateRender().
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Source):
					OnSourceChangedPartial(Source);
					break;
				case nameof(IsCenterHollow):
					OnIsCenterHollowChangedPartial(IsCenterHollow);
					break;
				case nameof(BottomInset):
					OnBottomInsetChangedPartial(BottomInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(LeftInset):
					OnLeftInsetChangedPartial(LeftInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(RightInset):
					OnRightInsetChangedPartial(RightInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(TopInset):
					OnTopInsetChangedPartial(TopInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(BottomInsetScale):
					OnBottomInsetScaleChangedPartial(BottomInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(LeftInsetScale):
					OnLeftInsetScaleChangedPartial(LeftInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(RightInsetScale):
					OnRightInsetScaleChangedPartial(RightInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(TopInsetScale):
					OnTopInsetScaleChangedPartial(TopInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				default:
					break;
			}
		}

		partial void OnSourceChangedPartial(CompositionBrush? source);
		partial void OnIsCenterHollowChangedPartial(bool isCenterHollow);

		partial void OnBottomInsetChangedPartial(float insest);
		partial void OnLeftInsetChangedPartial(float insest);
		partial void OnRightInsetChangedPartial(float insest);
		partial void OnTopInsetChangedPartial(float insest);

		partial void OnBottomInsetScaleChangedPartial(float scale);
		partial void OnLeftInsetScaleChangedPartial(float scale);
		partial void OnRightInsetScaleChangedPartial(float scale);
		partial void OnTopInsetScaleChangedPartial(float scale);

		partial void OnInsetOrScaleChangedPartial();

		public float BottomInset
		{
			get { return _bottomInset; }
			set { SetProperty(ref _bottomInset, value); }
		}

		public float BottomInsetScale
		{
			get { return _bottomInsetScale; }
			set { SetProperty(ref _bottomInsetScale, value); }
		}

		public float LeftInset
		{
			get { return _leftInset; }
			set { SetProperty(ref _leftInset, value); }
		}

		public float LeftInsetScale
		{
			get { return _leftInsetScale; }
			set { SetProperty(ref _leftInsetScale, value); }
		}

		public float RightInset
		{
			get { return _rightInset; }
			set { SetProperty(ref _rightInset, value); }
		}

		public float RightInsetScale
		{
			get { return _rightInsetScale; }
			set { SetProperty(ref _rightInsetScale, value); }
		}

		public float TopInset
		{
			get { return _topInset; }
			set { SetProperty(ref _topInset, value); }
		}

		public float TopInsetScale
		{
			get { return _topInsetScale; }
			set { SetProperty(ref _topInsetScale, value); }
		}

		public bool IsCenterHollow
		{
			get { return _isCenterHollow; }
			set { SetProperty(ref _isCenterHollow, value); }
		}

		public CompositionBrush? Source
		{
			get { return _source; }
			set { SetProperty(ref _source, value); }
		}

		public void SetInsets(float inset)
		{
			BottomInset = inset;
			LeftInset = inset;
			RightInset = inset;
			TopInset = inset;
		}

		public void SetInsets(float left, float top, float right, float bottom)
		{
			BottomInset = bottom;
			LeftInset = left;
			RightInset = right;
			TopInset = top;
		}

		public void SetInsetScales(float scale)
		{
			BottomInsetScale = scale;
			LeftInsetScale = scale;
			RightInsetScale = scale;
			TopInsetScale = scale;
		}

		public void SetInsetScales(float left, float top, float right, float bottom)
		{
			BottomInsetScale = bottom;
			LeftInsetScale = left;
			RightInsetScale = right;
			TopInsetScale = top;
		}

		private static readonly SKPaint _tempPaint = new();
		private SKBitmap? _bitmap;
		private SKCanvas? _bitmapCanvas;
		private SKRectI _insetRect;

		internal override bool RequiresRepaintOnEveryFrame => Source?.RequiresRepaintOnEveryFrame ?? false;
		internal override float DamageRegionSamplingMargin => Source?.DamageRegionSamplingMargin ?? 0;

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			if (Source is null)
			{
				return;
			}

			SKRect sourceBounds;
			if (Source is ISizedBrush sizedBrush && sizedBrush.Size is Vector2 sourceSize)
			{
				sourceBounds = new(0, 0, sourceSize.X, sourceSize.Y);
			}
			else
			{
				sourceBounds = bounds;
			}

			var newSize = new SKSizeI((int)sourceBounds.Width, (int)sourceBounds.Height);
			var info = new SKImageInfo(newSize.Width, newSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
			if (_bitmap is null || _bitmapCanvas is null || _bitmap.Info.Size != newSize)
			{
				_bitmap?.Dispose();
				_bitmapCanvas?.Dispose();
				_bitmap = new SKBitmap(info);
				_bitmapCanvas = new SKCanvas(_bitmap);
			}
			else
			{
				_bitmapCanvas.Clear(SKColors.Transparent);
			}

			Source.Paint(_bitmapCanvas, opacity, sourceBounds);
			_bitmapCanvas.Flush();
			var image = SKImage.FromPixels(info, _bitmap.GetPixels());

			_insetRect.Top = (int)(TopInset * TopInsetScale);
			_insetRect.Bottom = (int)(sourceBounds.Height - (BottomInset * BottomInsetScale));
			_insetRect.Right = (int)(sourceBounds.Width - (RightInset * RightInsetScale));
			_insetRect.Left = (int)(LeftInset * LeftInsetScale);

			_tempPaint.Reset();
			_tempPaint.IsAntialias = true;
			_tempPaint.IsDither = true;
			if (IsCenterHollow)
			{
				canvas.Save();
				canvas.ClipRect(_insetRect, SKClipOperation.Difference, antialias: true);
				canvas.DrawImageNinePatch(image, _insetRect, bounds, _tempPaint);
				canvas.Restore();
			}
			else
			{
				canvas.DrawBitmapNinePatch(_bitmap, _insetRect, bounds, _tempPaint);
			}
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
	}
}
