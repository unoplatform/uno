using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Effects;

#if !WINDOWS_UWP // Making the sample buildable on UWP
using Windows.Graphics.Effects.Interop;
#endif

using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition", Name = "CompositionEffectBrush", Description = "Paints a SpriteVisual with the output of a filter effect. The filter effect description is defined using the CompositionEffectFactory class.", IsManualTest = true)]
	public sealed partial class EffectBrushTests : UserControl
	{
		public EffectBrushTests()
		{
			this.InitializeComponent();
			this.Loaded += EffectBrushTests_Loaded;
		}

		private void EffectBrushTests_Loaded(object sender, RoutedEventArgs e)
		{
#if !WINDOWS_UWP
			var compositor = Windows.UI.Xaml.Window.Current.Compositor;

			var effect = new SimpleBlurEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), BlurAmount = 5.0f };
			var factory = compositor.CreateEffectFactory(effect);
			var effectBrush = factory.CreateBrush();

			blurGrid.Background = new EffectTesterBrush(effectBrush);

			var effect2 = new SimpleGrayscaleEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") };
			var factory2 = compositor.CreateEffectFactory(effect2);
			var effectBrush2 = factory2.CreateBrush();

			grayGrid.Background = new EffectTesterBrush(effectBrush2);

			var effect3 = new SimpleInvertEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") };
			var factory3 = compositor.CreateEffectFactory(effect3);
			var effectBrush3 = factory3.CreateBrush();

			invertGrid.Background = new EffectTesterBrush(effectBrush3);

			var effect4 = new SimpleHueRotationEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Angle = (float)MathEx.ToRadians(45) };
			var factory4 = compositor.CreateEffectFactory(effect4);
			var effectBrush4 = factory4.CreateBrush();

			hueGrid.Background = new EffectTesterBrush(effectBrush4);

			// BEGIN: Aggregation Test

			var effect5 = new SimpleGrayscaleEffect() { Source = effect };
			var factory5 = compositor.CreateEffectFactory(effect5);
			var effectBrush5 = factory5.CreateBrush();

			// END: Aggregation Test

			aggregationGrid.Background = new EffectTesterBrush(effectBrush5);

			var effect6 = new SimpleTintEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Color = Color.FromArgb(127, 66, 135, 245) };
			var factory6 = compositor.CreateEffectFactory(effect6);
			var effectBrush6 = factory6.CreateBrush();

			tintGrid.Background = new EffectTesterBrush(effectBrush6);

			var effect7 = new SimpleBlendEffect() { Background = new CompositionEffectSourceParameter("sourceBrush"), Foreground = new CompositionEffectSourceParameter("secondaryBrush"), Mode = D2D1BlendEffectMode.Color };
			var factory7 = compositor.CreateEffectFactory(effect7);
			var effectBrush7 = factory7.CreateBrush();

			blendGrid.Background = new EffectTesterBrushWithSecondaryBrush(effectBrush7, compositor.CreateColorBrush(Colors.LightBlue));

			var surface = LoadedImageSurface.StartLoadFromUri(new Uri("https://user-images.githubusercontent.com/34550324/266135095-71c9ce0a-4e49-408f-b2ff-670a53adef10.png"));
			surface.LoadCompleted += (s, o) =>
			{
				if (o.Status == LoadedImageSourceLoadStatus.Success)
				{
					var brush = compositor.CreateSurfaceBrush(surface);

					var effect8 = new SimpleCompositeEffect() { Sources = { new CompositionEffectSourceParameter("secondaryBrush"), new CompositionEffectSourceParameter("sourceBrush") }, Mode = D2D1CompositeMode.SourceOver };
					var factory8 = compositor.CreateEffectFactory(effect8);
					var effectBrush8 = factory8.CreateBrush();

					compositeGrid.Background = new EffectTesterBrushWithSecondaryBrush(effectBrush8, brush);
				}
			};

			var effect9 = new SimpleColorSourceEffect() { Color = Color.FromArgb(127, 66, 135, 245) };
			var factory9 = compositor.CreateEffectFactory(effect9);
			var effectBrush9 = factory9.CreateBrush();

			colorGrid.Background = new EffectTesterBrush(effectBrush9);

			var effect10 = new SimpleOpacityEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Opacity = 0.2f };
			var factory10 = compositor.CreateEffectFactory(effect10);
			var effectBrush10 = factory10.CreateBrush();

			opacityGrid.Background = new EffectTesterBrush(effectBrush10);

			var effect11 = new SimpleContrastEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Contrast = 2.0f };
			var factory11 = compositor.CreateEffectFactory(effect11);
			var effectBrush11 = factory11.CreateBrush();

			contrastGrid.Background = new EffectTesterBrush(effectBrush11);

			var effect12 = new SimpleExposureEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Exposure = -1.0f };
			var factory12 = compositor.CreateEffectFactory(effect12);
			var effectBrush12 = factory12.CreateBrush();

			exposureGrid.Background = new EffectTesterBrush(effectBrush12);

			var surface2 = LoadedImageSurface.StartLoadFromUri(new Uri("https://user-images.githubusercontent.com/34550324/266135095-71c9ce0a-4e49-408f-b2ff-670a53adef10.png"));
			surface2.LoadCompleted += (s, o) =>
			{
				if (o.Status == LoadedImageSourceLoadStatus.Success)
				{
					var brush = compositor.CreateSurfaceBrush(surface2);

					var effect13 = new SimpleCrossfadeEffect() { Source1 = new CompositionEffectSourceParameter("sourceBrush"), Source2 = new CompositionEffectSourceParameter("secondaryBrush"), CrossFade = 0.5f };
					var factory13 = compositor.CreateEffectFactory(effect13);
					var effectBrush13 = factory13.CreateBrush();

					crossfadeGrid.Background = new EffectTesterBrushWithSecondaryBrush(effectBrush13, brush);
				}
			};

			var effect14 = new SimpleLuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") };
			var factory14 = compositor.CreateEffectFactory(effect14);
			var effectBrush14 = factory14.CreateBrush();

			lumaGrid.Background = new EffectTesterBrush(effectBrush14);

			var effect15 = new SimpleLinearTransferEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), RedOffset = -1.0f, RedSlope = 2.5f, GreenOffset = -1.0f, GreenSlope = 5.0f };
			var factory15 = compositor.CreateEffectFactory(effect15);
			var effectBrush15 = factory15.CreateBrush();

			linearXferGrid.Background = new EffectTesterBrush(effectBrush15);

			var effect16 = new SimpleGammaTransferEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), RedExponent = 0.25f, GreenExponent = 0.25f, BlueExponent = 0.25f };
			var factory16 = compositor.CreateEffectFactory(effect16);
			var effectBrush16 = factory16.CreateBrush();

			gammaXferGrid.Background = new EffectTesterBrush(effectBrush16);

			var effect17 = new SimpleBorderEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Extend = D2D1BorderEdgeMode.Wrap };
			var factory17 = compositor.CreateEffectFactory(effect17);
			var effectBrush17 = factory17.CreateBrush();

			borderGrid.Background = new EffectTesterBrush(effectBrush17, 50);

			var effect18 = new SimpleTransform2DEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), TransformMatrix = Matrix3x2.CreateRotation((float)MathEx.ToRadians(45), new(100, 100)) };
			var factory18 = compositor.CreateEffectFactory(effect18);
			var effectBrush18 = factory18.CreateBrush();

			t2dGrid.Background = new EffectTesterBrush(effectBrush18);

			var effect19 = new SimpleSepiaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Intensity = 1.0f };
			var factory19 = compositor.CreateEffectFactory(effect19);
			var effectBrush19 = factory19.CreateBrush();

			sepiaGrid.Background = new EffectTesterBrush(effectBrush19);

			var effect20 = new SimpleTemperatureAndTintEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Temperature = 0.5f, Tint = 0.5f };
			var factory20 = compositor.CreateEffectFactory(effect20);
			var effectBrush20 = factory20.CreateBrush();

			tempTintGrid.Background = new EffectTesterBrush(effectBrush20);

			var swapRedAndBlue = new SimpleMatrix5x4
			{
				M11 = 0, M12 = 0, M13 = 1, M14 = 0,
				M21 = 0, M22 = 1, M23 = 0, M24 = 0,
				M31 = 1, M32 = 0, M33 = 0, M34 = 0,
				M41 = 0, M42 = 0, M43 = 0, M44 = 1,
				M51 = 0, M52 = 0, M53 = 0, M54 = 0
			};

			var effect21 = new SimpleColorMatrixEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), ColorMatrix = swapRedAndBlue };
			var factory21 = compositor.CreateEffectFactory(effect21);
			var effectBrush21 = factory21.CreateBrush();

			matrixGrid.Background = new EffectTesterBrush(effectBrush21);

			var surface3 = LoadedImageSurface.StartLoadFromUri(new Uri("https://user-images.githubusercontent.com/34550324/268126129-1bb2c5e7-24a4-41dc-bcdf-59f533c0d173.png"));
			surface3.LoadCompleted += (s, o) =>
			{
				if (o.Status == LoadedImageSourceLoadStatus.Success)
				{
					var brush = compositor.CreateSurfaceBrush(surface3);

					var effect22 = new SimpleDistantDiffuseEffect() { Source = new SimpleLuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, DiffuseAmount = 5.0f, Azimuth = (float)MathEx.ToRadians(180.0f) };
					var factory22 = compositor.CreateEffectFactory(effect22);
					var effectBrush22 = factory22.CreateBrush();

					effectBrush22.SetSourceParameter("sourceBrush", brush);

					ddGrid.Background = new XamlCompositionBrush(effectBrush22);

					var effect23 = new SimpleDistantSpecularEffect() { Source = new SimpleLuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, Azimuth = (float)MathEx.ToRadians(180.0f) };
					var factory23 = compositor.CreateEffectFactory(effect23);
					var effectBrush23 = factory23.CreateBrush();

					effectBrush23.SetSourceParameter("sourceBrush", brush);

					dsGrid.Background = new XamlCompositionBrush(effectBrush23);

					var effect24 = new SimpleSpotDiffuseEffect() { Source = new SimpleLuminanceToAlphaEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") }, DiffuseAmount = 5.0f, LimitingConeAngle = 0.25f, LightTarget = new Vector3(surface3.DecodedSize.ToVector2(), 0) / 2 };
					var factory24 = compositor.CreateEffectFactory(effect24);
					var effectBrush24 = factory24.CreateBrush();

					effectBrush24.SetSourceParameter("sourceBrush", brush);

					sdGrid.Background = new XamlCompositionBrush(effectBrush24);
				}
			};
#endif
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
				var compositor = Windows.UI.Xaml.Window.Current.Compositor;
				var surface = LoadedImageSurface.StartLoadFromUri(new Uri($"https://avatars.githubusercontent.com/u/52228309?s={_imgSize}&v=4"));
				surface.LoadCompleted += (s, o) =>
				{
					if (o.Status == LoadedImageSourceLoadStatus.Success)
					{
						var brush = compositor.CreateSurfaceBrush(surface);

						_effectBrush.SetSourceParameter("sourceBrush", brush);
						CompositionBrush = _effectBrush;
					}
				};
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
				var compositor = Windows.UI.Xaml.Window.Current.Compositor;
				var surface = LoadedImageSurface.StartLoadFromUri(new Uri("https://avatars.githubusercontent.com/u/52228309?s=200&v=4"));
				surface.LoadCompleted += (s, o) =>
				{
					if (o.Status == LoadedImageSourceLoadStatus.Success)
					{
						var brush = compositor.CreateSurfaceBrush(surface);

						_effectBrush.SetSourceParameter("sourceBrush", brush);
						_effectBrush.SetSourceParameter("secondaryBrush", _secondaryBrush);
						CompositionBrush = _effectBrush;
					}
				};
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

#if !WINDOWS_UWP
		[Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5")]
		private class SimpleBlurEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleBlurEffect";
			private Guid _id = new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public IGraphicsEffectSource Source { get; set; }

			public float BlurAmount { get; set; } = 3.0f;

			public uint Optimization { get; set; } // enum

			public uint BorderMode { get; set; } // enum

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "BlurAmount":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "Optimization":
						{
							index = 1;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BorderMode":
						{
							index = 2;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return BlurAmount;
					case 1:
						return Optimization;
					case 2:
						return BorderMode;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 3;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("36DDE0EB-3725-42E0-836D-52FB20AEE644")]
		private class SimpleGrayscaleEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleGrayscaleEffect";
			private Guid _id = new Guid("36DDE0EB-3725-42E0-836D-52FB20AEE644");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping) => throw new NotSupportedException();

			public object GetProperty(uint index) => throw new NotSupportedException();

			public uint GetPropertyCount() => 0;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("E0C3784D-CB39-4E84-B6FD-6B72F0810263")]
		private class SimpleInvertEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleInvertEffect";
			private Guid _id = new Guid("E0C3784D-CB39-4E84-B6FD-6B72F0810263");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping) => throw new NotSupportedException();

			public object GetProperty(uint index) => throw new NotSupportedException();

			public uint GetPropertyCount() => 0;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("0F4458EC-4B32-491B-9E85-BD73F44D3EB6")]
		private class SimpleHueRotationEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleHueRotationEffect";
			private Guid _id = new Guid("0F4458EC-4B32-491B-9E85-BD73F44D3EB6");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Angle { get; set; } = 0.0f; // Radians

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Angle":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Angle;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("36312B17-F7DD-4014-915D-FFCA768CF211")]
		private class SimpleTintEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleTintEffect";
			private Guid _id = new Guid("36312B17-F7DD-4014-915D-FFCA768CF211");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public Color Color { get; set; } = Color.FromArgb(255, 255, 255, 255);

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Color":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.ColorToVector4;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Color;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("81C5B77B-13F8-4CDD-AD20-C890547AC65D")]
		private class SimpleBlendEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleBlendEffect";
			private Guid _id = new Guid("81C5B77B-13F8-4CDD-AD20-C890547AC65D");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public D2D1BlendEffectMode Mode { get; set; } = D2D1BlendEffectMode.Multiply;

			public IGraphicsEffectSource Background { get; set; }

			public IGraphicsEffectSource Foreground { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Mode":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Mode;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;

			public IGraphicsEffectSource GetSource(uint index) => index is 0 ? Background : Foreground;

			public uint GetSourceCount() => 2;
		}

		[Guid("48FC9F51-F6AC-48F1-8B58-3B28AC46F76D")]
		private class SimpleCompositeEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleCompositeEffect";
			private Guid _id = new Guid("48FC9F51-F6AC-48F1-8B58-3B28AC46F76D");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public D2D1CompositeMode Mode { get; set; } = D2D1CompositeMode.SourceOver;

			public List<IGraphicsEffectSource> Sources { get; set; } = new();

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Mode":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Mode;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;

			public IGraphicsEffectSource GetSource(uint index) => index < Sources.Count ? Sources[(int)index] : null;

			public uint GetSourceCount() => (uint)Sources.Count;
		}

		[Guid("61C23C20-AE69-4D8E-94CF-50078DF638F2")]
		private class SimpleColorSourceEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleColorSourceEffect";
			private Guid _id = new Guid("61C23C20-AE69-4D8E-94CF-50078DF638F2");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public Color Color { get; set; } = Color.FromArgb(255, 255, 255, 255);

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Color":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.ColorToVector4;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Color;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => throw new InvalidOperationException();
			public uint GetSourceCount() => 0;
		}

		[Guid("811D79A4-DE28-4454-8094-C64685F8BD4C")]
		private class SimpleOpacityEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleOpacityEffect";
			private Guid _id = new Guid("811D79A4-DE28-4454-8094-C64685F8BD4C");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Opacity { get; set; } = 1.0f;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Opacity":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Opacity;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("B648A78A-0ED5-4F80-A94A-8E825ACA6B77")]
		private class SimpleContrastEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleContrastEffect";
			private Guid _id = new Guid("B648A78A-0ED5-4F80-A94A-8E825ACA6B77");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Contrast { get; set; } = 0.0f;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Contrast":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Contrast;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("B56C8CFA-F634-41EE-BEE0-FFA617106004")]
		private class SimpleExposureEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleExposureEffect";
			private Guid _id = new Guid("B56C8CFA-F634-41EE-BEE0-FFA617106004");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Exposure { get; set; } = 0.0f;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Exposure":
					case "ExposureValue":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Exposure;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("12F575E8-4DB1-485F-9A84-03A07DD3829F")]
		private class SimpleCrossfadeEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleCrossfadeEffect";
			private Guid _id = new Guid("12F575E8-4DB1-485F-9A84-03A07DD3829F");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float CrossFade { get; set; } = 0.5f;

			public IGraphicsEffectSource Source1 { get; set; }

			public IGraphicsEffectSource Source2 { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Weight":
					case "CrossFade":
					case "Crossfade":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return CrossFade;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => index is 0 ? Source1 : Source2;
			public uint GetSourceCount() => 2;
		}

		[Guid("41251AB7-0BEB-46F8-9DA7-59E93FCCE5DE")]
		private class SimpleLuminanceToAlphaEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleLuminanceToAlphaEffect";
			private Guid _id = new Guid("41251AB7-0BEB-46F8-9DA7-59E93FCCE5DE");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping) => throw new NotSupportedException();

			public object GetProperty(uint index) => throw new NotSupportedException();

			public uint GetPropertyCount() => 0;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("AD47C8FD-63EF-4ACC-9B51-67979C036C06")]
		private class SimpleLinearTransferEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleLinearTransferEffect";
			private Guid _id = new Guid("AD47C8FD-63EF-4ACC-9B51-67979C036C06");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float RedOffset { get; set; } = 0.0f;

			public float RedSlope { get; set; } = 1.0f;

			public bool RedDisable { get; set; } = false;


			public float GreenOffset { get; set; } = 0.0f;

			public float GreenSlope { get; set; } = 1.0f;

			public bool GreenDisable { get; set; } = false;


			public float BlueOffset { get; set; } = 0.0f;

			public float BlueSlope { get; set; } = 1.0f;

			public bool BlueDisable { get; set; } = false;


			public float AlphaOffset { get; set; } = 0.0f;

			public float AlphaSlope { get; set; } = 1.0f;

			public bool AlphaDisable { get; set; } = false;


			public bool ClampOutput { get; set; } = false;


			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "RedOffset":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "RedSlope":
						{
							index = 1;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "RedDisable":
						{
							index = 2;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "GreenOffset":
						{
							index = 3;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "GreenSlope":
						{
							index = 4;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "GreenDisable":
						{
							index = 5;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BlueOffset":
						{
							index = 6;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BlueSlope":
						{
							index = 7;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BlueDisable":
						{
							index = 8;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "AlphaOffset":
						{
							index = 9;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "AlphaSlope":
						{
							index = 10;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "AlphaDisable":
						{
							index = 11;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "ClampOutput":
						{
							index = 12;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return RedOffset;
					case 1:
						return RedSlope;
					case 2:
						return RedDisable;
					case 3:
						return GreenOffset;
					case 4:
						return GreenSlope;
					case 5:
						return GreenDisable;
					case 6:
						return BlueOffset;
					case 7:
						return BlueSlope;
					case 8:
						return BlueDisable;
					case 9:
						return AlphaOffset;
					case 10:
						return AlphaSlope;
					case 11:
						return AlphaDisable;
					case 12:
						return ClampOutput;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 13;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("409444C4-C419-41A0-B0C1-8CD0C0A18E42")]
		private class SimpleGammaTransferEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleGammaTransferEffect";
			private Guid _id = new Guid("409444C4-C419-41A0-B0C1-8CD0C0A18E42");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float RedAmplitude { get; set; } = 1.0f;

			public float RedExponent { get; set; } = 1.0f;

			public float RedOffset { get; set; } = 0.0f;

			public bool RedDisable { get; set; } = false;


			public float GreenAmplitude { get; set; } = 1.0f;

			public float GreenExponent { get; set; } = 1.0f;

			public float GreenOffset { get; set; } = 0.0f;

			public bool GreenDisable { get; set; } = false;


			public float BlueAmplitude { get; set; } = 1.0f;

			public float BlueExponent { get; set; } = 1.0f;

			public float BlueOffset { get; set; } = 0.0f;

			public bool BlueDisable { get; set; } = false;


			public float AlphaAmplitude { get; set; } = 1.0f;

			public float AlphaExponent { get; set; } = 1.0f;

			public float AlphaOffset { get; set; } = 0.0f;

			public bool AlphaDisable { get; set; } = false;


			public bool ClampOutput { get; set; } = false;


			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "RedAmplitude":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "RedExponent":
						{
							index = 1;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "RedOffset":
						{
							index = 2;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "RedDisable":
						{
							index = 3;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "GreenAmplitude":
						{
							index = 4;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "GreenExponent":
						{
							index = 5;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "GreenOffset":
						{
							index = 6;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "GreenDisable":
						{
							index = 7;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BlueAmplitude":
						{
							index = 8;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BlueExponent":
						{
							index = 9;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BlueOffset":
						{
							index = 10;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "BlueDisable":
						{
							index = 11;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "AlphaAmplitude":
						{
							index = 12;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "AlphaExponent":
						{
							index = 13;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "AlphaOffset":
						{
							index = 14;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "AlphaDisable":
						{
							index = 15;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "ClampOutput":
						{
							index = 16;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return RedAmplitude;
					case 1:
						return RedExponent;
					case 2:
						return RedOffset;
					case 3:
						return RedDisable;
					case 4:
						return GreenAmplitude;
					case 5:
						return GreenExponent;
					case 6:
						return GreenOffset;
					case 7:
						return GreenDisable;
					case 8:
						return BlueAmplitude;
					case 9:
						return BlueExponent;
					case 10:
						return BlueOffset;
					case 11:
						return BlueDisable;
					case 12:
						return AlphaAmplitude;
					case 13:
						return AlphaExponent;
					case 14:
						return AlphaOffset;
					case 15:
						return AlphaDisable;
					case 16:
						return ClampOutput;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 17;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("2A2D49C0-4ACF-43C7-8C6A-7C4A27874D27")]
		private class SimpleBorderEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleBorderEffect";
			private Guid _id = new Guid("2A2D49C0-4ACF-43C7-8C6A-7C4A27874D27");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public D2D1BorderEdgeMode Extend { get; set; } = D2D1BorderEdgeMode.Clamp;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "ExtendX":
					case "ExtendY":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Extend;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 2;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("6AA97485-6354-4CFC-908C-E4A74F62C96C")]
		private class SimpleTransform2DEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleTransform2DEffect";
			private Guid _id = new Guid("6AA97485-6354-4CFC-908C-E4A74F62C96C");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public Matrix3x2 TransformMatrix { get; set; } = Matrix3x2.Identity;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "TransformMatrix":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return TransformMatrix;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 4;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("3A1AF410-5F1D-4DBE-84DF-915DA79B7153")]
		private class SimpleSepiaEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleSepiaEffect";
			private Guid _id = new Guid("3A1AF410-5F1D-4DBE-84DF-915DA79B7153");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Intensity { get; set; } = 0.5f;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Intensity":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Intensity;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("89176087-8AF9-4A08-AEB1-895F38DB1766")]
		private class SimpleTemperatureAndTintEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleTemperatureAndTintEffect";
			private Guid _id = new Guid("89176087-8AF9-4A08-AEB1-895F38DB1766");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Temperature { get; set; } = 0.0f;

			public float Tint { get; set; } = 0.0f;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Temperature":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "Tint":
						{
							index = 1;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Temperature;
					case 1:
						return Tint;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 2;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		private struct SimpleMatrix5x4
		{
			public float M11;
			public float M12;
			public float M13;
			public float M14;
			public float M21;
			public float M22;
			public float M23;
			public float M24;
			public float M31;
			public float M32;
			public float M33;
			public float M34;
			public float M41;
			public float M42;
			public float M43;
			public float M44;
			public float M51;
			public float M52;
			public float M53;
			public float M54;

			public static SimpleMatrix5x4 Identity
			{
				get => new SimpleMatrix5x4()
				{
					M11 = 1, M12 = 0, M13 = 0, M14 = 0,
					M21 = 0, M22 = 1, M23 = 0, M24 = 0,
					M31 = 0, M32 = 0, M33 = 1, M34 = 0,
					M41 = 0, M42 = 0, M43 = 0, M44 = 1,
					M51 = 0, M52 = 0, M53 = 0, M54 = 0
				};
			}

			public float[] ToArray()
			{
				return new float[20]
				{
					M11, M12, M13, M14,
					M21, M22, M23, M24,
					M31, M32, M33, M34,
					M41, M42, M43, M44,
					M51, M52, M53, M54
				};
			}
		}

		[Guid("921F03D6-641C-47DF-852D-B4BB6153AE11")]
		private class SimpleColorMatrixEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleColorMatrixEffect";
			private Guid _id = new Guid("921F03D6-641C-47DF-852D-B4BB6153AE11");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public SimpleMatrix5x4 ColorMatrix { get; set; } = SimpleMatrix5x4.Identity;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "ColorMatrix":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return ColorMatrix.ToArray();
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 1;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("3E7EFD62-A32D-46D4-A83C-5278889AC954")]
		private class SimpleDistantDiffuseEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleDistantDiffuseEffect";
			private Guid _id = new Guid("3E7EFD62-A32D-46D4-A83C-5278889AC954");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Azimuth { get; set; } = 0.0f;

			public float Elevation { get; set; } = 0.0f;

			public float DiffuseAmount { get; set; } = 1.0f;

			public Color LightColor { get; set; } = Colors.White;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Azimuth":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
							break;
						}
					case "Elevation":
						{
							index = 1;
							mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
							break;
						}
					case "DiffuseAmount":
						{
							index = 2;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "LightColor":
						{
							index = 3;
							mapping = GraphicsEffectPropertyMapping.ColorToVector3;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Azimuth;
					case 1:
						return Elevation;
					case 2:
						return DiffuseAmount;
					case 3:
						return LightColor;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 4;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("428C1EE5-77B8-4450-8AB5-72219C21ABDA")]
		private class SimpleDistantSpecularEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleDistantSpecularEffect";
			private Guid _id = new Guid("428C1EE5-77B8-4450-8AB5-72219C21ABDA");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public float Azimuth { get; set; } = 0.0f;

			public float Elevation { get; set; } = 0.0f;

			public float SpecularExponent { get; set; } = 1.0f;

			public float SpecularAmount { get; set; } = 1.0f;

			public Color LightColor { get; set; } = Colors.White;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "Azimuth":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
							break;
						}
					case "Elevation":
						{
							index = 1;
							mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
							break;
						}
					case "SpecularExponent":
						{
							index = 2;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "SpecularAmount":
						{
							index = 3;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "LightColor":
						{
							index = 4;
							mapping = GraphicsEffectPropertyMapping.ColorToVector3;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return Azimuth;
					case 1:
						return Elevation;
					case 2:
						return SpecularExponent;
					case 3:
						return SpecularAmount;
					case 4:
						return LightColor;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 5;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}

		[Guid("818A1105-7932-44F4-AA86-08AE7B2F2C93")]
		private class SimpleSpotDiffuseEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
		{
			private string _name = "SimpleSpotDiffuseEffect";
			private Guid _id = new Guid("818A1105-7932-44F4-AA86-08AE7B2F2C93");

			public string Name
			{
				get => _name;
				set => _name = value;
			}

			public Vector3 LightPosition { get; set; } = new();

			public Vector3 LightTarget { get; set; } = new();

			public float Focus { get; set; } = 1.0f;

			public float LimitingConeAngle { get; set; } = MathF.PI / 2.0f;

			public float DiffuseAmount { get; set; } = 1.0f;

			public Color LightColor { get; set; } = Colors.White;

			public IGraphicsEffectSource Source { get; set; }

			public Guid GetEffectId() => _id;

			public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
			{
				switch (name)
				{
					case "LightPosition":
						{
							index = 0;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "LightTarget":
						{
							index = 1;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "Focus":
						{
							index = 2;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "LimitingConeAngle":
						{
							index = 3;
							mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
							break;
						}
					case "DiffuseAmount":
						{
							index = 4;
							mapping = GraphicsEffectPropertyMapping.Direct;
							break;
						}
					case "LightColor":
						{
							index = 5;
							mapping = GraphicsEffectPropertyMapping.ColorToVector3;
							break;
						}
					default:
						{
							index = 0xFF;
							mapping = (GraphicsEffectPropertyMapping)0xFF;
							break;
						}
				}
			}

			public object GetProperty(uint index)
			{
				switch (index)
				{
					case 0:
						return LightPosition;
					case 1:
						return LightTarget;
					case 2:
						return Focus;
					case 3:
						return LimitingConeAngle;
					case 4:
						return DiffuseAmount;
					case 5:
						return LightColor;
					default:
						return null;
				}
			}

			public uint GetPropertyCount() => 6;
			public IGraphicsEffectSource GetSource(uint index) => Source;
			public uint GetSourceCount() => 1;
		}
#endif
	}
}
