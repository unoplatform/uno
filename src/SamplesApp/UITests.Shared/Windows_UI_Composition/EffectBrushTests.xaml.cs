using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ShimSkiaSharp;
using SkiaSharp;
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
			var compositor = Window.Current.Compositor;

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

			// Aggregation Test

			var effect5 = new SimpleGrayscaleEffect() { Source = effect };
			var factory5 = compositor.CreateEffectFactory(effect5);
			var effectBrush5 = factory5.CreateBrush();

			aggregationGrid.Background = new EffectTesterBrush(effectBrush5);

			var effect6 = new SimpleTintEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), Color = Color.FromArgb(127, 66, 135, 245) };
			var factory6 = compositor.CreateEffectFactory(effect6);
			var effectBrush6 = factory6.CreateBrush();

			tintGrid.Background = new EffectTesterBrush(effectBrush6);

			var effect7 = new SimpleBlendEffect() { Background = new CompositionEffectSourceParameter("sourceBrush"), Foreground = new CompositionEffectSourceParameter("secondaryBrush"), Mode = D2D1BlendEffectMode.Color };
			var factory7 = compositor.CreateEffectFactory(effect7);
			var effectBrush7 = factory7.CreateBrush();

			blendGrid.Background = new EffectTesterBrushWithSecondaryBrush(effectBrush7, compositor.CreateColorBrush(Colors.LightBlue));

			var surface = LoadedImageSurface.StartLoadFromUri(new Uri("https://upload.wikimedia.org/wikipedia/commons/thumb/e/ee/Logo-winui.svg/200px-Logo-winui.svg.png"));
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
#endif
		}

		private class EffectTesterBrush : XamlCompositionBrushBase
		{
			private CompositionEffectBrush _effectBrush;

			public EffectTesterBrush(CompositionEffectBrush effectBrush) => _effectBrush = effectBrush;

			protected override void OnConnected()
			{
				var compositor = Window.Current.Compositor;
				var surface = LoadedImageSurface.StartLoadFromUri(new Uri("https://avatars.githubusercontent.com/u/52228309?s=200&v=4"));
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
				var compositor = Window.Current.Compositor;
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
#endif
	}
}
