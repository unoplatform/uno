using System;
using System.Numerics;
using Uno.UI.Samples.Controls;

using Windows.UI;

#if WINDOWS_UWP
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Composition.Effects;
using XamlWindow = Windows.UI.Xaml.Window;
#else
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Composition.Effects;
using XamlWindow = Windows.UI.Xaml.Window;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition", Name = "CompositionEffectBrush", Description = "Paints a SpriteVisual with the output of a filter effect. The filter effect description is defined using the CompositionEffectFactory class.", IsManualTest = true)]
	public sealed partial class EffectBrushTests : UserControl
	{
		private static CompositionSurfaceBrush _unoBrush;

		public EffectBrushTests()
		{
			this.InitializeComponent();
			this.Loaded += EffectBrushTests_Loaded;
			this.Unloaded += EffectBrushTests_Unloaded;
		}

		private void EffectBrushTests_Loaded(object sender, RoutedEventArgs e)
		{
#if HAS_UNO
			var compositor = XamlWindow.Current.Compositor;

			var effect = new GaussianBlurEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), BlurAmount = 5.0f };
			var factory = compositor.CreateEffectFactory(effect);
			var effectBrush = factory.CreateBrush();

			blurGrid.Background = new EffectTesterBrush(effectBrush);

			var effect2 = new GrayscaleEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") };
			var factory2 = compositor.CreateEffectFactory(effect2);
			var effectBrush2 = factory2.CreateBrush();

			grayGrid.Background = new EffectTesterBrush(effectBrush2);

			var effect3 = new InvertEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") };
			var factory3 = compositor.CreateEffectFactory(effect3);
			var effectBrush3 = factory3.CreateBrush();

			invertGrid.Background = new EffectTesterBrush(effectBrush3);

			var effect4 = new HueRotationEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Angle = (float)Uno.Extensions.MathEx.ToRadians(45) };
			var factory4 = compositor.CreateEffectFactory(effect4);
			var effectBrush4 = factory4.CreateBrush();

			hueGrid.Background = new EffectTesterBrush(effectBrush4);

			// BEGIN: Aggregation Test

			var effect5 = new GrayscaleEffect() { Source = effect };
			var factory5 = compositor.CreateEffectFactory(effect5);
			var effectBrush5 = factory5.CreateBrush();

			// END: Aggregation Test

			aggregationGrid.Background = new EffectTesterBrush(effectBrush5);

			var effect6 = new TintEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Color = Color.FromArgb(127, 66, 135, 245) };
			var factory6 = compositor.CreateEffectFactory(effect6);
			var effectBrush6 = factory6.CreateBrush();

			tintGrid.Background = new EffectTesterBrush(effectBrush6);

			var effect7 = new BlendEffect() { Background = new CompositionEffectSourceParameter("sourceBrush"), Foreground = new CompositionEffectSourceParameter("secondaryBrush"), Mode = BlendEffectMode.Luminosity /* this is actually BlendEffectMode.Color, see comment on D2D1BlendEffectMode*/ };
			var factory7 = compositor.CreateEffectFactory(effect7);
			var effectBrush7 = factory7.CreateBrush();

			blendGrid.Background = new EffectTesterBrushWithSecondaryBrush(effectBrush7, compositor.CreateColorBrush(Colors.LightBlue));

			var surface = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/WinUI.png"));
			surface.LoadCompleted += (s, o) =>
			{
				if (o.Status == LoadedImageSourceLoadStatus.Success)
				{
					var brush = compositor.CreateSurfaceBrush(surface);

					var effect8 = new CompositeEffect() { Sources = { new CompositionEffectSourceParameter("secondaryBrush"), new CompositionEffectSourceParameter("sourceBrush") }, Mode = CanvasComposite.SourceOver };
					var factory8 = compositor.CreateEffectFactory(effect8);
					var effectBrush8 = factory8.CreateBrush();

					compositeGrid.Background = new EffectTesterBrushWithSecondaryBrush(effectBrush8, brush);
				}
			};

			var effect9 = new ColorSourceEffect() { Color = Color.FromArgb(127, 66, 135, 245) };
			var factory9 = compositor.CreateEffectFactory(effect9);
			var effectBrush9 = factory9.CreateBrush();

			colorGrid.Background = new EffectTesterBrush(effectBrush9);

			var effect10 = new OpacityEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Opacity = 0.2f };
			var factory10 = compositor.CreateEffectFactory(effect10);
			var effectBrush10 = factory10.CreateBrush();

			opacityGrid.Background = new EffectTesterBrush(effectBrush10);

			var effect11 = new ContrastEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Contrast = 2.0f };
			var factory11 = compositor.CreateEffectFactory(effect11);
			var effectBrush11 = factory11.CreateBrush();

			contrastGrid.Background = new EffectTesterBrush(effectBrush11);

			var effect12 = new ExposureEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Exposure = -1.0f };
			var factory12 = compositor.CreateEffectFactory(effect12);
			var effectBrush12 = factory12.CreateBrush();

			exposureGrid.Background = new EffectTesterBrush(effectBrush12);

			var surface2 = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/WinUI.png"));
			surface2.LoadCompleted += (s, o) =>
			{
				if (o.Status == LoadedImageSourceLoadStatus.Success)
				{
					var brush = compositor.CreateSurfaceBrush(surface2);

					var effect13 = new CrossFadeEffect() { Source1 = new CompositionEffectSourceParameter("sourceBrush"), Source2 = new CompositionEffectSourceParameter("secondaryBrush"), CrossFade = 0.5f };
					var factory13 = compositor.CreateEffectFactory(effect13);
					var effectBrush13 = factory13.CreateBrush();

					crossfadeGrid.Background = new EffectTesterBrushWithSecondaryBrush(effectBrush13, brush);
				}
			};

			var effect14 = new LuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") };
			var factory14 = compositor.CreateEffectFactory(effect14);
			var effectBrush14 = factory14.CreateBrush();

			lumaGrid.Background = new EffectTesterBrush(effectBrush14);

			var effect15 = new LinearTransferEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), RedOffset = -1.0f, RedSlope = 2.5f, GreenOffset = -1.0f, GreenSlope = 5.0f };
			var factory15 = compositor.CreateEffectFactory(effect15);
			var effectBrush15 = factory15.CreateBrush();

			linearXferGrid.Background = new EffectTesterBrush(effectBrush15);

			var effect16 = new GammaTransferEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), RedExponent = 0.25f, GreenExponent = 0.25f, BlueExponent = 0.25f };
			var factory16 = compositor.CreateEffectFactory(effect16);
			var effectBrush16 = factory16.CreateBrush();

			gammaXferGrid.Background = new EffectTesterBrush(effectBrush16);

			var effect17 = new BorderEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), ExtendX = CanvasEdgeBehavior.Wrap };
			var factory17 = compositor.CreateEffectFactory(effect17);
			var effectBrush17 = factory17.CreateBrush();

			borderGrid.Background = new EffectTesterBrush(effectBrush17, 50);

			var effect18 = new Transform2DEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), TransformMatrix = Matrix3x2.CreateRotation((float)Uno.Extensions.MathEx.ToRadians(45), new(100, 100)) };
			var factory18 = compositor.CreateEffectFactory(effect18);
			var effectBrush18 = factory18.CreateBrush();

			t2dGrid.Background = new EffectTesterBrush(effectBrush18);

			var effect19 = new SepiaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Intensity = 1.0f };
			var factory19 = compositor.CreateEffectFactory(effect19);
			var effectBrush19 = factory19.CreateBrush();

			sepiaGrid.Background = new EffectTesterBrush(effectBrush19);

			var effect20 = new TemperatureAndTintEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Temperature = 0.5f, Tint = 0.5f };
			var factory20 = compositor.CreateEffectFactory(effect20);
			var effectBrush20 = factory20.CreateBrush();

			tempTintGrid.Background = new EffectTesterBrush(effectBrush20);

			var swapRedAndBlue = new Matrix5x4
			{
				M11 = 0,
				M12 = 0,
				M13 = 1,
				M14 = 0,
				M21 = 0,
				M22 = 1,
				M23 = 0,
				M24 = 0,
				M31 = 1,
				M32 = 0,
				M33 = 0,
				M34 = 0,
				M41 = 0,
				M42 = 0,
				M43 = 0,
				M44 = 1,
				M51 = 0,
				M52 = 0,
				M53 = 0,
				M54 = 0
			};

			var effect21 = new ColorMatrixEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), ColorMatrix = swapRedAndBlue };
			var factory21 = compositor.CreateEffectFactory(effect21);
			var effectBrush21 = factory21.CreateBrush();

			matrixGrid.Background = new EffectTesterBrush(effectBrush21);

			var surface3 = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/LightMap.png"));
			surface3.LoadCompleted += (s, o) =>
			{
				if (o.Status == LoadedImageSourceLoadStatus.Success)
				{
					var brush = compositor.CreateSurfaceBrush(surface3);

					var effect22 = new DistantDiffuseEffect() { Source = new LuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, DiffuseAmount = 5.0f, Azimuth = (float)Uno.Extensions.MathEx.ToRadians(180.0f) };
					var factory22 = compositor.CreateEffectFactory(effect22);
					var effectBrush22 = factory22.CreateBrush();

					effectBrush22.SetSourceParameter("sourceBrush", brush);

					ddGrid.Background = new XamlCompositionBrush(effectBrush22);

					var effect23 = new DistantSpecularEffect() { Source = new LuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, Azimuth = (float)Uno.Extensions.MathEx.ToRadians(180.0f) };
					var factory23 = compositor.CreateEffectFactory(effect23);
					var effectBrush23 = factory23.CreateBrush();

					effectBrush23.SetSourceParameter("sourceBrush", brush);

					dsGrid.Background = new XamlCompositionBrush(effectBrush23);

					var effect24 = new SpotDiffuseEffect() { Source = new LuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, DiffuseAmount = 5.0f, LimitingConeAngle = 0.25f, LightTarget = new Vector3(surface3.DecodedSize.ToVector2(), 0) / 2 };
					var factory24 = compositor.CreateEffectFactory(effect24);
					var effectBrush24 = factory24.CreateBrush();

					effectBrush24.SetSourceParameter("sourceBrush", brush);

					sdGrid.Background = new XamlCompositionBrush(effectBrush24);

					var effect25 = new SpotSpecularEffect() { Source = new LuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, SpecularAmount = 1.25f, LimitingConeAngle = 0.25f, LightTarget = new Vector3(surface3.DecodedSize.ToVector2(), 0) / 2, LightColor = Colors.WhiteSmoke };
					var factory25 = compositor.CreateEffectFactory(effect25);
					var effectBrush25 = factory25.CreateBrush();

					effectBrush25.SetSourceParameter("sourceBrush", brush);

					ssGrid.Background = new XamlCompositionBrush(effectBrush25);

					var effect26 = new PointDiffuseEffect() { Source = new LuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, DiffuseAmount = 1.25f, LightColor = Colors.WhiteSmoke };
					var factory26 = compositor.CreateEffectFactory(effect26);
					var effectBrush26 = factory26.CreateBrush();

					effectBrush26.SetSourceParameter("sourceBrush", brush);

					pdGrid.Background = new XamlCompositionBrush(effectBrush26);

					var effect27 = new PointSpecularEffect() { Source = new LuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, SpecularAmount = 1.25f, LightColor = Colors.WhiteSmoke };
					var factory27 = compositor.CreateEffectFactory(effect27);
					var effectBrush27 = factory27.CreateBrush();

					effectBrush27.SetSourceParameter("sourceBrush", brush);

					psGrid.Background = new XamlCompositionBrush(effectBrush27);
				}
			};

			var effect28 = new AlphaMaskEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), AlphaMask = new CompositionEffectSourceParameter("maskBrush") };
			var factory28 = compositor.CreateEffectFactory(effect28);
			var effectBrush28 = factory28.CreateBrush();

			var maskBrush = compositor.CreateLinearGradientBrush();
			maskBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, Colors.Black));
			maskBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.75f, Colors.Transparent));
			maskBrush.StartPoint = new(0.0f, 0.0f);
			maskBrush.EndPoint = new(0.0f, 200.0f);

			effectBrush28.SetSourceParameter("maskBrush", maskBrush);

			maskGrid.Background = new EffectTesterBrush(effectBrush28);

			var effect29 = new SaturationEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Saturation = 0.3f };
			var factory29 = compositor.CreateEffectFactory(effect29);
			var effectBrush29 = factory29.CreateBrush();

			saturationGrid.Background = new EffectTesterBrush(effectBrush29);

			var effect30 = new WhiteNoiseEffect() { Frequency = new(1.04f) };
			var factory30 = compositor.CreateEffectFactory(effect30);
			var effectBrush30 = factory30.CreateBrush();

			noiseGrid.Background = new XamlCompositionBrush(effectBrush30);
#endif
		}

		private void EffectBrushTests_Unloaded(object sender, RoutedEventArgs e)
		{
			_unoBrush?.Dispose();
			_unoBrush = null;
		}

		private class EffectTesterBrush : XamlCompositionBrushBase
		{
			private CompositionEffectBrush _effectBrush;
			private int _imgSize;

			public EffectTesterBrush(CompositionEffectBrush effectBrush, int imgSize = 200)
			{
				_effectBrush = effectBrush;
				_imgSize = imgSize;
			}

			protected override void OnConnected()
			{
				var compositor = XamlWindow.Current.Compositor;

				if (_unoBrush is null || _imgSize != 200)
				{
					var surface = LoadedImageSurface.StartLoadFromUri(new Uri($"ms-appx:///Assets/Uno{_imgSize}x{_imgSize}.png"));
					surface.LoadCompleted += (s, o) =>
					{
						if (o.Status == LoadedImageSourceLoadStatus.Success)
						{
							CompositionSurfaceBrush brush;
							if (_imgSize != 200)
							{
								brush = compositor.CreateSurfaceBrush(surface);
							}
							else
							{
								_unoBrush = brush = compositor.CreateSurfaceBrush(surface);
							}

							_effectBrush.SetSourceParameter("sourceBrush", brush);
							CompositionBrush = _effectBrush;
						}
					};
				}
				else
				{
					_effectBrush.SetSourceParameter("sourceBrush", _unoBrush);
					CompositionBrush = _effectBrush;
				}
			}
		}

		private class EffectTesterBrushWithSecondaryBrush : XamlCompositionBrushBase
		{
			private CompositionEffectBrush _effectBrush;
			private CompositionBrush _secondaryBrush;

			public EffectTesterBrushWithSecondaryBrush(CompositionEffectBrush effectBrush, CompositionBrush secondaryBrush)
			{
				_effectBrush = effectBrush;
				_secondaryBrush = secondaryBrush;
			}

			protected override void OnConnected()
			{
				var compositor = XamlWindow.Current.Compositor;

				if (_unoBrush is null)
				{
					var surface = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/Uno200x200.png"));
					surface.LoadCompleted += (s, o) =>
					{
						if (o.Status == LoadedImageSourceLoadStatus.Success)
						{
							_unoBrush = compositor.CreateSurfaceBrush(surface);

							_effectBrush.SetSourceParameter("sourceBrush", _unoBrush);
							_effectBrush.SetSourceParameter("secondaryBrush", _secondaryBrush);
							CompositionBrush = _effectBrush;
						}
					};
				}
				else
				{
					_effectBrush.SetSourceParameter("sourceBrush", _unoBrush);
					_effectBrush.SetSourceParameter("secondaryBrush", _secondaryBrush);
					CompositionBrush = _effectBrush;
				}
			}
		}

		private class XamlCompositionBrush : XamlCompositionBrushBase
		{
			CompositionBrush _brush;

			public XamlCompositionBrush(CompositionBrush brush) => _brush = brush;

			protected override void OnConnected()
			{
				CompositionBrush = _brush;
			}
		}
	}
}
