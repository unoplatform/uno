using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if __SKIA__
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Primitives;
using SkiaSharp;
#endif

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", Name = "SkiaVisualShowcase")]
	public sealed partial class SkiaVisualShowcase : UserControl
	{
		public int MaxSampleIndex => SkiaWrapper.VisualCount - 1;

		public SkiaVisualShowcase()
		{
			this.InitializeComponent();
		}
	}

	public class SkiaWrapper : FrameworkElement
	{
#if __SKIA__
		private SkiaVisual _skiaVisual;
		private static readonly List<Type> _visuals = new List<Type>
		{
			typeof(SkiaVisual1), typeof(SkiaVisual2), typeof(SkiaVisual3),
		};
		public static int VisualCount { get; } = _visuals.Count;
#else
		public static int VisualCount { get; } = 0;
#endif

		public SkiaWrapper()
		{
			SampleChanged(0);
		}

		public static DependencyProperty SampleProperty { get; } = DependencyProperty.Register(
			nameof(Sample),
			typeof(int),
			typeof(SkiaWrapper),
			new PropertyMetadata(-1, (o, args) => ((SkiaWrapper)o).SampleChanged((int)args.NewValue)));

		public int Sample
		{
			get => (int)GetValue(SampleProperty);
			set => SetValue(SampleProperty, value);
		}

		private void SampleChanged(int newIndex)
		{
#if __SKIA__
			var coercedIndex = Math.Min(Math.Max(0, newIndex), _visuals.Count - 1);
			if (coercedIndex != Sample)
			{
				Sample = coercedIndex;
			}
			else
			{
				_skiaVisual = (SkiaVisual)Activator.CreateInstance(_visuals[coercedIndex], Visual.Compositor);
				ElementCompositionPreview.SetElementChildVisual(this, _skiaVisual!);

				InvalidateArrange();
				// don't wait for a rendering cycle to update the visual's Size, or else there will be
				// a split second where the visual will have invalid clipping
				Arrange(LayoutInformation.GetLayoutSlot(this));
			}
#endif
		}

#if __SKIA__
		protected override Size MeasureOverride(Size availableSize) => availableSize;

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_skiaVisual is { })
			{
				_skiaVisual.Size = new Vector2((float)finalSize.Width, (float)finalSize.Height);
				_skiaVisual.Clip = _skiaVisual.Compositor.CreateRectangleClip(0, 0, (float)finalSize.Width, (float)finalSize.Height);
				ApplyFlowDirection((float)finalSize.Width);
			}

			return base.ArrangeOverride(finalSize);
		}

		private void ApplyFlowDirection(float width)
		{
			if (FlowDirection == FlowDirection.RightToLeft)
			{
				_skiaVisual.TransformMatrix = new Matrix4x4(new Matrix3x2(-1.0f, 0.0f, 0.0f, 1.0f, width, 0.0f));
			}
			else
			{
				_skiaVisual.TransformMatrix = Matrix4x4.Identity;
			}
		}
#endif
	}

#if __SKIA__
	public class SkiaVisual1(Compositor compositor) : SkiaVisual(compositor)
	{
		// https://fiddle.skia.org/c/@shapes
		protected override void Invalidate(SKCanvas canvas)
		{
			canvas.DrawColor(SKColors.White);

			var paint = new SKPaint();
			paint.Style = SKPaintStyle.Fill;
			paint.IsAntialias = true;
			paint.StrokeWidth = 4;
			paint.Color = new SKColor(0xff4285F4);

			var rect = SKRect.Create(10, 10, 100, 160);
			canvas.DrawRect(rect, paint);

			var oval = new SKPath();
			oval.AddRoundRect(rect, 20, 20);
			oval.Offset(new SKPoint(40, 80));
			paint.Color = new SKColor(0xffDB4437);
			canvas.DrawPath(oval, paint);

			paint.Color = new SKColor(0xff0F9D58);
			canvas.DrawCircle(180, 50, 25, paint);

			rect.Offset(80, 50);
			paint.Color = new SKColor(0xffF4B400);
			paint.Style = SKPaintStyle.Stroke;
			canvas.DrawRoundRect(rect, 10, 10, paint);
		}
	}

	public class SkiaVisual2(Compositor compositor) : SkiaVisual(compositor)
	{
		// https://fiddle.skia.org/c/@bezier_curves
		protected override void Invalidate(SKCanvas canvas)
		{
			canvas.DrawColor(SKColors.White);

			var paint = new SKPaint();
			paint.Style = SKPaintStyle.Stroke;
			paint.StrokeWidth = 8;
			paint.Color = new SKColor(0xff4285F4);
			paint.IsAntialias = true;
			paint.StrokeCap = SKStrokeCap.Round;

			var path = new SKPath();
			path.MoveTo(10, 10);
			path.QuadTo(256, 64, 128, 128);
			path.QuadTo(10, 192, 250, 250);
			canvas.DrawPath(path, paint);
		}
	}

	public class SkiaVisual3(Compositor compositor) : SkiaVisual(compositor)
	{
		// https://fiddle.skia.org/c/@shader
		protected override void Invalidate(SKCanvas canvas)
		{
			var paint = new SKPaint();
			using var pathEffect = SKPathEffect.CreateDiscrete(10.0f, 4.0f);
			paint.PathEffect = pathEffect;
			SKPoint[] points =
			{
				new SKPoint(0.0f, 0.0f),
				new SKPoint(256.0f, 256.0f)
			};
			SKColor[] colors =
			{
				new SKColor(66, 133, 244),
				new SKColor(15, 157, 88)
			};
			paint.Shader = SKShader.CreateLinearGradient(points[0], points[1], colors, SKShaderTileMode.Clamp);
			paint.IsAntialias = true;
			canvas.Clear(SKColors.White);
			var path = Star();
			canvas.DrawPath(path, paint);
		}

		private SKPath Star()
		{
			const float R = 60.0f, C = 128.0f;
			var path = new SKPath();
			path.MoveTo(C + R, C);
			for (var i = 1; i < 15; ++i)
			{
				var a = 0.44879895f * i;
				var r = R + R * (i % 2);
				path.LineTo((float)(C + r * Math.Cos(a)), (float)(C + r * Math.Sin(a)));
			}
			return path;
		}
	}
#endif
}
