using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
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
			var compositor = Window.Current.Compositor;

			var effect = new SimpleBlurEffect() { Source = new CompositionEffectSourceParameter("sourceBrush"), BlurAmount = 5.0f };
			var factory = compositor.CreateEffectFactory(effect);
			var effectBrush = factory.CreateBrush();

			blurGrid.Background = new EffectTesterBrush(effectBrush);

			var effect2 = new SimpleGrayscaleEffect() { Source = new CompositionEffectSourceParameter("sourceBrush") };
			var factory2 = compositor.CreateEffectFactory(effect2);
			var effectBrush2 = factory2.CreateBrush();

			grayGrid.Background = new EffectTesterBrush(effectBrush2);
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

#if WINDOWS_UWP
		private interface IGraphicsEffectD2D1Interop { }
#endif

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
	}
}
