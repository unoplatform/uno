using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas;
using System.Numerics;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", Name = "CompositionPath", Description = "Represents a series of connected lines and curves.", IsManualTest = true)]
	public sealed partial class CompositionPathTests : UserControl
	{
		public CompositionPathTests()
		{
			this.InitializeComponent();
			this.Loaded += CompositionPathTests_Loaded;
		}

		private void CompositionPathTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = Microsoft.UI.Xaml.Window.Current.Compositor;
			var device = CanvasDevice.GetSharedDevice();

			// Simple shape
			using (var builder = new CanvasPathBuilder(device))
			{
				var visual = compositor.CreateShapeVisual();

				builder.BeginFigure(1, 1);
				builder.AddLine(200, 200);
				builder.AddLine(1, 200);
				builder.EndFigure(CanvasFigureLoop.Closed);

				var path = new CompositionPath(CanvasGeometry.CreatePath(builder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(Windows.UI.Colors.LightGreen);

				visual.Shapes.Add(shape);
				visual.Size = new(200);

				ElementCompositionPreview.SetElementChildVisual(compPresenter1, visual);
			}

			// Complex shape
			using (var builder = new CanvasPathBuilder(device))
			{
				var visual = compositor.CreateShapeVisual();

				builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				builder.BeginFigure(new Vector2(656.5f, 400.5f));
				builder.AddCubicBezier(new Vector2(656.5f, 350.637f), new Vector2(598.572f, 307.493f), new Vector2(514.292f, 286.708f));
				builder.AddCubicBezier(new Vector2(493.507f, 202.428f), new Vector2(450.363f, 144.5f), new Vector2(400.5f, 144.5f));
				builder.AddCubicBezier(new Vector2(350.637f, 144.5f), new Vector2(307.493f, 202.428f), new Vector2(286.708f, 286.708f));
				builder.AddCubicBezier(new Vector2(202.428f, 307.493f), new Vector2(144.5f, 350.637f), new Vector2(144.5f, 400.5f));
				builder.AddCubicBezier(new Vector2(144.5f, 450.363f), new Vector2(202.428f, 493.507f), new Vector2(286.708f, 514.292f));
				builder.AddCubicBezier(new Vector2(307.493f, 598.572f), new Vector2(350.637f, 656.5f), new Vector2(400.5f, 656.5f));
				builder.AddCubicBezier(new Vector2(450.363f, 656.5f), new Vector2(493.507f, 598.572f), new Vector2(514.292f, 514.292f));
				builder.AddCubicBezier(new Vector2(598.572f, 493.507f), new Vector2(656.5f, 450.363f), new Vector2(656.5f, 400.5f));
				builder.EndFigure(CanvasFigureLoop.Closed);

				builder.BeginFigure(new Vector2(581.519f, 219.481f));
				builder.AddCubicBezier(new Vector2(546.261f, 184.222f), new Vector2(474.793f, 194.676f), new Vector2(400.5f, 239.574f));
				builder.AddCubicBezier(new Vector2(326.207f, 194.676f), new Vector2(254.739f, 184.222f), new Vector2(219.481f, 219.481f));
				builder.AddCubicBezier(new Vector2(184.222f, 254.739f), new Vector2(194.676f, 326.207f), new Vector2(239.574f, 400.5f));
				builder.AddCubicBezier(new Vector2(194.676f, 474.792f), new Vector2(184.222f, 546.261f), new Vector2(219.481f, 581.519f));
				builder.AddCubicBezier(new Vector2(254.739f, 616.778f), new Vector2(326.207f, 606.324f), new Vector2(400.5f, 561.426f));
				builder.AddCubicBezier(new Vector2(474.793f, 606.324f), new Vector2(546.261f, 616.778f), new Vector2(581.519f, 581.519f));
				builder.AddCubicBezier(new Vector2(616.778f, 546.261f), new Vector2(606.324f, 474.792f), new Vector2(561.426f, 400.5f));
				builder.AddCubicBezier(new Vector2(606.324f, 326.207f), new Vector2(616.778f, 254.739f), new Vector2(581.519f, 219.481f));
				builder.EndFigure(CanvasFigureLoop.Closed);

				builder.BeginFigure(new Vector2(148.5f, 112.5f));
				builder.AddLine(new Vector2(646.5f, 112.5f));
				builder.AddLine(new Vector2(646.5f, 112.5f));
				builder.AddArc(new Vector2(682.5f, 148.5f), 36, 36, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
				builder.AddLine(new Vector2(682.5f, 646.5f));
				builder.AddLine(new Vector2(682.5f, 646.5f));
				builder.AddArc(new Vector2(646.5f, 682.5f), 36, 36, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
				builder.AddLine(new Vector2(148.5f, 682.5f));
				builder.AddLine(new Vector2(148.5f, 682.5f));
				builder.AddArc(new Vector2(112.5f, 646.5f), 36, 36, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
				builder.AddLine(new Vector2(112.5f, 148.5f));
				builder.AddLine(new Vector2(112.5f, 148.5f));
				builder.AddArc(new Vector2(148.5f, 112.5f), 36, 36, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
				builder.EndFigure(CanvasFigureLoop.Closed);

				var path = new CompositionPath(CanvasGeometry.CreatePath(builder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(Windows.UI.Colors.Transparent);
				shape.StrokeBrush = compositor.CreateColorBrush(Windows.UI.Colors.LightGreen);
				shape.StrokeThickness = 0.8f;
				shape.Scale = new(200f / 570f);
				shape.Offset = -112.5f * shape.Scale;

				visual.Shapes.Add(shape);
				visual.Size = new(200);

				ElementCompositionPreview.SetElementChildVisual(compPresenter2, visual);
			}

			// Multiple shapes, source: https://www.svgrepo.com
			var multiShapeVisual = compositor.CreateShapeVisual();

			// 1
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(41.84375f, 26f));
				pathBuilder.AddLine(new Vector2(159.0313f, 26f));
				pathBuilder.AddLine(new Vector2(159.0313f, 180.2969f));
				pathBuilder.AddCubicBezier(new Vector2(159.0313f, 183.5313f), new Vector2(156.4102f, 186.1563f), new Vector2(153.1719f, 186.1563f));
				pathBuilder.AddLine(new Vector2(47.70313f, 186.1563f));
				pathBuilder.AddCubicBezier(new Vector2(44.46875f, 186.1563f), new Vector2(41.84375f, 183.5313f), new Vector2(41.84375f, 180.2969f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(41.84375f, 26f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 223, 237, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 2
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(153.1719f, 187.6211f));
				pathBuilder.AddLine(new Vector2(47.70313f, 187.6211f));
				pathBuilder.AddCubicBezier(new Vector2(43.66016f, 187.6172f), new Vector2(40.38672f, 184.3398f), new Vector2(40.38281f, 180.2969f));
				pathBuilder.AddLine(new Vector2(40.38281f, 26f));
				pathBuilder.AddCubicBezier(new Vector2(40.38281f, 25.19141f), new Vector2(41.03516f, 24.53516f), new Vector2(41.84375f, 24.53516f));
				pathBuilder.AddLine(new Vector2(159.0313f, 24.53516f));
				pathBuilder.AddCubicBezier(new Vector2(159.8438f, 24.53516f), new Vector2(160.5f, 25.19141f), new Vector2(160.5f, 26f));
				pathBuilder.AddLine(new Vector2(160.5f, 180.2969f));
				pathBuilder.AddCubicBezier(new Vector2(160.4922f, 184.3398f), new Vector2(157.2188f, 187.6172f), new Vector2(153.1758f, 187.6211f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(43.3125f, 27.46484f));
				pathBuilder.AddLine(new Vector2(43.3125f, 180.2969f));
				pathBuilder.AddCubicBezier(new Vector2(43.3125f, 182.7227f), new Vector2(45.28125f, 184.6875f), new Vector2(47.70313f, 184.6914f));
				pathBuilder.AddLine(new Vector2(153.1719f, 184.6914f));
				pathBuilder.AddCubicBezier(new Vector2(155.5977f, 184.6875f), new Vector2(157.5664f, 182.7227f), new Vector2(157.5703f, 180.2969f));
				pathBuilder.AddLine(new Vector2(157.5703f, 27.46484f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(43.3125f, 27.46484f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 3
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(47.70313f, 14.16016f));
				pathBuilder.AddLine(new Vector2(153.1719f, 14.16016f));
				pathBuilder.AddCubicBezier(new Vector2(156.4102f, 14.16016f), new Vector2(159.0313f, 16.78516f), new Vector2(159.0313f, 20.01953f));
				pathBuilder.AddLine(new Vector2(159.0313f, 53.22266f));
				pathBuilder.AddLine(new Vector2(41.84375f, 53.22266f));
				pathBuilder.AddLine(new Vector2(41.84375f, 20.01953f));
				pathBuilder.AddCubicBezier(new Vector2(41.84375f, 16.78516f), new Vector2(44.46875f, 14.16016f), new Vector2(47.70313f, 14.16016f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(47.70313f, 14.16016f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 255, 255, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 4
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(159.0313f, 54.6875f));
				pathBuilder.AddLine(new Vector2(41.84375f, 54.6875f));
				pathBuilder.AddCubicBezier(new Vector2(41.03516f, 54.6875f), new Vector2(40.38281f, 54.03125f), new Vector2(40.38281f, 53.22266f));
				pathBuilder.AddLine(new Vector2(40.38281f, 20.01953f));
				pathBuilder.AddCubicBezier(new Vector2(40.38672f, 15.97656f), new Vector2(43.66016f, 12.69922f), new Vector2(47.70313f, 12.69531f));
				pathBuilder.AddLine(new Vector2(153.1719f, 12.69531f));
				pathBuilder.AddCubicBezier(new Vector2(157.2188f, 12.69922f), new Vector2(160.4922f, 15.97656f), new Vector2(160.5f, 20.01953f));
				pathBuilder.AddLine(new Vector2(160.5f, 53.22266f));
				pathBuilder.AddCubicBezier(new Vector2(160.5f, 54.03125f), new Vector2(159.8438f, 54.6875f), new Vector2(159.0313f, 54.6875f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(43.3125f, 51.75781f));
				pathBuilder.AddLine(new Vector2(157.5703f, 51.75781f));
				pathBuilder.AddLine(new Vector2(157.5703f, 20.01953f));
				pathBuilder.AddCubicBezier(new Vector2(157.5664f, 17.59375f), new Vector2(155.5977f, 15.62891f), new Vector2(153.1758f, 15.625f));
				pathBuilder.AddLine(new Vector2(47.70313f, 15.625f));
				pathBuilder.AddCubicBezier(new Vector2(45.28125f, 15.62891f), new Vector2(43.3125f, 17.59375f), new Vector2(43.3125f, 20.01953f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(43.3125f, 51.75781f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 5
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(61.375f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(61.375f, 132.7422f), new Vector2(78.86719f, 150.2344f), new Vector2(100.4375f, 150.2344f));
				pathBuilder.AddCubicBezier(new Vector2(122.0117f, 150.2344f), new Vector2(139.5f, 132.7422f), new Vector2(139.5f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(139.5f, 89.59766f), new Vector2(122.0117f, 72.10938f), new Vector2(100.4375f, 72.10938f));
				pathBuilder.AddCubicBezier(new Vector2(78.86719f, 72.10938f), new Vector2(61.375f, 89.59766f), new Vector2(61.375f, 111.1719f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(61.375f, 111.1719f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 255, 255, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 6
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(100.4375f, 151.6953f));
				pathBuilder.AddCubicBezier(new Vector2(78.09375f, 151.6953f), new Vector2(59.91406f, 133.5156f), new Vector2(59.91406f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(59.91406f, 88.82422f), new Vector2(78.09375f, 70.64453f), new Vector2(100.4375f, 70.64453f));
				pathBuilder.AddCubicBezier(new Vector2(122.7891f, 70.64453f), new Vector2(140.9688f, 88.82422f), new Vector2(140.9688f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(140.9688f, 133.5195f), new Vector2(122.7852f, 151.6953f), new Vector2(100.4375f, 151.6953f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(100.4375f, 73.57422f));
				pathBuilder.AddCubicBezier(new Vector2(79.70703f, 73.57422f), new Vector2(62.84375f, 90.44141f), new Vector2(62.84375f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(62.84375f, 131.8984f), new Vector2(79.70703f, 148.7656f), new Vector2(100.4375f, 148.7656f));
				pathBuilder.AddCubicBezier(new Vector2(121.1719f, 148.7656f), new Vector2(138.0391f, 131.8984f), new Vector2(138.0391f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(138.0391f, 90.44141f), new Vector2(121.1719f, 73.57422f), new Vector2(100.4375f, 73.57422f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(100.4375f, 73.57422f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 7
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(71.14063f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(71.14063f, 127.3516f), new Vector2(84.25781f, 140.4688f), new Vector2(100.4375f, 140.4688f));
				pathBuilder.AddCubicBezier(new Vector2(116.6211f, 140.4688f), new Vector2(129.7344f, 127.3516f), new Vector2(129.7344f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(129.7344f, 94.98828f), new Vector2(116.6211f, 81.875f), new Vector2(100.4375f, 81.875f));
				pathBuilder.AddCubicBezier(new Vector2(84.25781f, 81.875f), new Vector2(71.14063f, 94.98828f), new Vector2(71.14063f, 111.1719f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(71.14063f, 111.1719f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 223, 237, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 8
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(100.457f, 140.9531f));
				pathBuilder.AddLine(new Vector2(100.4375f, 140.9531f));
				pathBuilder.AddCubicBezier(new Vector2(98.49609f, 140.957f), new Vector2(96.55469f, 140.7656f), new Vector2(94.64844f, 140.3906f));
				pathBuilder.AddCubicBezier(new Vector2(94.38281f, 140.3398f), new Vector2(94.21094f, 140.082f), new Vector2(94.26563f, 139.8164f));
				pathBuilder.AddCubicBezier(new Vector2(94.32031f, 139.5508f), new Vector2(94.57813f, 139.3789f), new Vector2(94.84375f, 139.4336f));
				pathBuilder.AddCubicBezier(new Vector2(96.69141f, 139.7969f), new Vector2(98.56641f, 139.9805f), new Vector2(100.4453f, 139.9766f));
				pathBuilder.AddCubicBezier(new Vector2(100.7188f, 139.9766f), new Vector2(100.9375f, 140.1953f), new Vector2(100.9414f, 140.4688f));
				pathBuilder.AddCubicBezier(new Vector2(100.9453f, 140.5977f), new Vector2(100.8945f, 140.7227f), new Vector2(100.8047f, 140.8125f));
				pathBuilder.AddCubicBezier(new Vector2(100.7109f, 140.9063f), new Vector2(100.5859f, 140.957f), new Vector2(100.457f, 140.9531f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(106.168f, 140.3945f));
				pathBuilder.AddCubicBezier(new Vector2(105.8984f, 140.4219f), new Vector2(105.6602f, 140.2227f), new Vector2(105.6367f, 139.9531f));
				pathBuilder.AddCubicBezier(new Vector2(105.6094f, 139.6836f), new Vector2(105.8047f, 139.4453f), new Vector2(106.0781f, 139.418f));
				pathBuilder.AddCubicBezier(new Vector2(107.9258f, 139.0508f), new Vector2(109.7344f, 138.5f), new Vector2(111.4766f, 137.7813f));
				pathBuilder.AddCubicBezier(new Vector2(111.7266f, 137.6758f), new Vector2(112.0117f, 137.793f), new Vector2(112.1172f, 138.043f));
				pathBuilder.AddCubicBezier(new Vector2(112.2227f, 138.293f), new Vector2(112.1016f, 138.5781f), new Vector2(111.8555f, 138.6797f));
				pathBuilder.AddCubicBezier(new Vector2(110.0508f, 139.4297f), new Vector2(108.1797f, 139.9961f), new Vector2(106.2656f, 140.375f));
				pathBuilder.AddCubicBezier(new Vector2(106.2344f, 140.3867f), new Vector2(106.2031f, 140.3906f), new Vector2(106.168f, 140.3945f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(89.24609f, 138.7422f));
				pathBuilder.AddCubicBezier(new Vector2(89.17969f, 138.7422f), new Vector2(89.11328f, 138.7305f), new Vector2(89.05078f, 138.7031f));
				pathBuilder.AddCubicBezier(new Vector2(87.25f, 137.957f), new Vector2(85.52344f, 137.0352f), new Vector2(83.90234f, 135.9531f));
				pathBuilder.AddCubicBezier(new Vector2(83.67969f, 135.8008f), new Vector2(83.61719f, 135.4961f), new Vector2(83.76563f, 135.2734f));
				pathBuilder.AddCubicBezier(new Vector2(83.91797f, 135.0508f), new Vector2(84.22266f, 134.9883f), new Vector2(84.44531f, 135.1367f));
				pathBuilder.AddCubicBezier(new Vector2(86.01563f, 136.1875f), new Vector2(87.68359f, 137.0781f), new Vector2(89.42578f, 137.8008f));
				pathBuilder.AddCubicBezier(new Vector2(89.64453f, 137.8906f), new Vector2(89.76563f, 138.1211f), new Vector2(89.71875f, 138.3516f));
				pathBuilder.AddCubicBezier(new Vector2(89.66797f, 138.582f), new Vector2(89.46484f, 138.7461f), new Vector2(89.23047f, 138.7383f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(116.7266f, 136.0078f));
				pathBuilder.AddCubicBezier(new Vector2(116.5117f, 136.0078f), new Vector2(116.3203f, 135.8672f), new Vector2(116.2578f, 135.6602f));
				pathBuilder.AddCubicBezier(new Vector2(116.1953f, 135.4531f), new Vector2(116.2773f, 135.2305f), new Vector2(116.4531f, 135.1094f));
				pathBuilder.AddCubicBezier(new Vector2(118.0234f, 134.0625f), new Vector2(119.4844f, 132.8594f), new Vector2(120.8164f, 131.5234f));
				pathBuilder.AddCubicBezier(new Vector2(121.0078f, 131.3359f), new Vector2(121.3164f, 131.3359f), new Vector2(121.5078f, 131.5234f));
				pathBuilder.AddCubicBezier(new Vector2(121.6992f, 131.7148f), new Vector2(121.6992f, 132.0273f), new Vector2(121.5078f, 132.2188f));
				pathBuilder.AddCubicBezier(new Vector2(120.1289f, 133.6016f), new Vector2(118.6133f, 134.8477f), new Vector2(116.9922f, 135.9375f));
				pathBuilder.AddCubicBezier(new Vector2(116.9141f, 135.9883f), new Vector2(116.8203f, 136.0156f), new Vector2(116.7266f, 136.0156f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(79.74219f, 132.3906f));
				pathBuilder.AddCubicBezier(new Vector2(79.61328f, 132.3906f), new Vector2(79.48828f, 132.3398f), new Vector2(79.39453f, 132.25f));
				pathBuilder.AddCubicBezier(new Vector2(78.01563f, 130.8711f), new Vector2(76.77344f, 129.3594f), new Vector2(75.68359f, 127.7422f));
				pathBuilder.AddCubicBezier(new Vector2(75.53516f, 127.5156f), new Vector2(75.59375f, 127.2148f), new Vector2(75.82031f, 127.0625f));
				pathBuilder.AddCubicBezier(new Vector2(76.04297f, 126.9102f), new Vector2(76.34375f, 126.9727f), new Vector2(76.49609f, 127.1953f));
				pathBuilder.AddCubicBezier(new Vector2(77.54688f, 128.7617f), new Vector2(78.74609f, 130.2227f), new Vector2(80.07813f, 131.5547f));
				pathBuilder.AddCubicBezier(new Vector2(80.21875f, 131.6953f), new Vector2(80.26172f, 131.9063f), new Vector2(80.18359f, 132.0898f));
				pathBuilder.AddCubicBezier(new Vector2(80.10938f, 132.2734f), new Vector2(79.92969f, 132.3906f), new Vector2(79.73438f, 132.3906f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(124.8047f, 127.9297f));
				pathBuilder.AddCubicBezier(new Vector2(124.625f, 127.9297f), new Vector2(124.457f, 127.832f), new Vector2(124.375f, 127.6719f));
				pathBuilder.AddCubicBezier(new Vector2(124.2891f, 127.5117f), new Vector2(124.2969f, 127.3203f), new Vector2(124.3984f, 127.1719f));
				pathBuilder.AddCubicBezier(new Vector2(125.4492f, 125.6016f), new Vector2(126.3398f, 123.9336f), new Vector2(127.0625f, 122.1914f));
				pathBuilder.AddCubicBezier(new Vector2(127.1289f, 122.0273f), new Vector2(127.2734f, 121.9102f), new Vector2(127.4492f, 121.8867f));
				pathBuilder.AddCubicBezier(new Vector2(127.625f, 121.8633f), new Vector2(127.8008f, 121.9336f), new Vector2(127.9063f, 122.0742f));
				pathBuilder.AddCubicBezier(new Vector2(128.0117f, 122.2148f), new Vector2(128.0352f, 122.4023f), new Vector2(127.9648f, 122.5664f));
				pathBuilder.AddCubicBezier(new Vector2(127.2188f, 124.3672f), new Vector2(126.293f, 126.0898f), new Vector2(125.2109f, 127.7109f));
				pathBuilder.AddCubicBezier(new Vector2(125.1172f, 127.8477f), new Vector2(124.9688f, 127.9297f), new Vector2(124.8047f, 127.9297f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(73.375f, 122.8984f));
				pathBuilder.AddCubicBezier(new Vector2(73.17969f, 122.8984f), new Vector2(73f, 122.7813f), new Vector2(72.92578f, 122.5977f));
				pathBuilder.AddCubicBezier(new Vector2(72.17578f, 120.7969f), new Vector2(71.60938f, 118.9258f), new Vector2(71.22656f, 117.0117f));
				pathBuilder.AddCubicBezier(new Vector2(71.17188f, 116.7461f), new Vector2(71.34375f, 116.4883f), new Vector2(71.60547f, 116.4336f));
				pathBuilder.AddCubicBezier(new Vector2(71.87109f, 116.3828f), new Vector2(72.12891f, 116.5508f), new Vector2(72.18359f, 116.8164f));
				pathBuilder.AddCubicBezier(new Vector2(72.55078f, 118.668f), new Vector2(73.10156f, 120.4805f), new Vector2(73.82813f, 122.2227f));
				pathBuilder.AddCubicBezier(new Vector2(73.89063f, 122.375f), new Vector2(73.875f, 122.5469f), new Vector2(73.78516f, 122.6797f));
				pathBuilder.AddCubicBezier(new Vector2(73.69141f, 122.8164f), new Vector2(73.53906f, 122.8984f), new Vector2(73.375f, 122.8984f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(129.1797f, 117.3711f));
				pathBuilder.AddCubicBezier(new Vector2(129.1484f, 117.375f), new Vector2(129.1172f, 117.375f), new Vector2(129.0859f, 117.3711f));
				pathBuilder.AddCubicBezier(new Vector2(128.957f, 117.3477f), new Vector2(128.8438f, 117.2734f), new Vector2(128.7695f, 117.1641f));
				pathBuilder.AddCubicBezier(new Vector2(128.6953f, 117.0586f), new Vector2(128.668f, 116.9258f), new Vector2(128.6953f, 116.7969f));
				pathBuilder.AddCubicBezier(new Vector2(129.0586f, 114.9453f), new Vector2(129.2422f, 113.0625f), new Vector2(129.2383f, 111.1797f));
				pathBuilder.AddLine(new Vector2(129.2383f, 111.1094f));
				pathBuilder.AddCubicBezier(new Vector2(129.2383f, 110.8398f), new Vector2(129.457f, 110.6211f), new Vector2(129.7266f, 110.6211f));
				pathBuilder.AddCubicBezier(new Vector2(129.9961f, 110.6211f), new Vector2(130.2148f, 110.8398f), new Vector2(130.2148f, 111.1094f));
				pathBuilder.AddLine(new Vector2(130.2148f, 111.1797f));
				pathBuilder.AddCubicBezier(new Vector2(130.2148f, 113.1289f), new Vector2(130.0273f, 115.0742f), new Vector2(129.6484f, 116.9883f));
				pathBuilder.AddCubicBezier(new Vector2(129.6016f, 117.207f), new Vector2(129.4063f, 117.3672f), new Vector2(129.1797f, 117.3711f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(71.14063f, 111.6914f));
				pathBuilder.AddCubicBezier(new Vector2(70.87109f, 111.6914f), new Vector2(70.65234f, 111.4727f), new Vector2(70.65234f, 111.2031f));
				pathBuilder.AddLine(new Vector2(70.65234f, 111.1719f));
				pathBuilder.AddCubicBezier(new Vector2(70.65234f, 109.2305f), new Vector2(70.83984f, 107.2969f), new Vector2(71.21484f, 105.3945f));
				pathBuilder.AddCubicBezier(new Vector2(71.26563f, 105.1289f), new Vector2(71.52344f, 104.957f), new Vector2(71.78906f, 105.0117f));
				pathBuilder.AddCubicBezier(new Vector2(72.05469f, 105.0664f), new Vector2(72.22656f, 105.3242f), new Vector2(72.17188f, 105.5898f));
				pathBuilder.AddCubicBezier(new Vector2(71.8125f, 107.4297f), new Vector2(71.62891f, 109.3008f), new Vector2(71.63281f, 111.1797f));
				pathBuilder.AddLine(new Vector2(71.63281f, 111.2109f));
				pathBuilder.AddCubicBezier(new Vector2(71.625f, 111.4766f), new Vector2(71.41016f, 111.6914f), new Vector2(71.14063f, 111.6914f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(129.1641f, 105.875f));
				pathBuilder.AddCubicBezier(new Vector2(128.9336f, 105.8789f), new Vector2(128.7344f, 105.7148f), new Vector2(128.6875f, 105.4844f));
				pathBuilder.AddCubicBezier(new Vector2(128.3164f, 103.6367f), new Vector2(127.7656f, 101.8281f), new Vector2(127.0391f, 100.0898f));
				pathBuilder.AddCubicBezier(new Vector2(126.9648f, 99.92578f), new Vector2(126.9844f, 99.73828f), new Vector2(127.0938f, 99.59375f));
				pathBuilder.AddCubicBezier(new Vector2(127.1992f, 99.45313f), new Vector2(127.375f, 99.37891f), new Vector2(127.5508f, 99.40234f));
				pathBuilder.AddCubicBezier(new Vector2(127.7266f, 99.42578f), new Vector2(127.875f, 99.54297f), new Vector2(127.9375f, 99.71094f));
				pathBuilder.AddCubicBezier(new Vector2(128.6914f, 101.5117f), new Vector2(129.2617f, 103.3828f), new Vector2(129.6445f, 105.293f));
				pathBuilder.AddCubicBezier(new Vector2(129.6758f, 105.4375f), new Vector2(129.6367f, 105.5859f), new Vector2(129.543f, 105.7031f));
				pathBuilder.AddCubicBezier(new Vector2(129.4531f, 105.8164f), new Vector2(129.3125f, 105.8828f), new Vector2(129.1641f, 105.8828f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(73.35156f, 100.4805f));
				pathBuilder.AddCubicBezier(new Vector2(73.1875f, 100.4805f), new Vector2(73.03516f, 100.3984f), new Vector2(72.94531f, 100.2617f));
				pathBuilder.AddCubicBezier(new Vector2(72.85547f, 100.1289f), new Vector2(72.83594f, 99.95703f), new Vector2(72.89844f, 99.80469f));
				pathBuilder.AddCubicBezier(new Vector2(73.64453f, 98f), new Vector2(74.56641f, 96.27734f), new Vector2(75.64844f, 94.65625f));
				pathBuilder.AddCubicBezier(new Vector2(75.80078f, 94.42969f), new Vector2(76.10547f, 94.37109f), new Vector2(76.32813f, 94.51953f));
				pathBuilder.AddCubicBezier(new Vector2(76.55078f, 94.66797f), new Vector2(76.61328f, 94.97266f), new Vector2(76.46094f, 95.19922f));
				pathBuilder.AddCubicBezier(new Vector2(75.41406f, 96.76563f), new Vector2(74.52734f, 98.43359f), new Vector2(73.80469f, 100.1797f));
				pathBuilder.AddCubicBezier(new Vector2(73.73047f, 100.3633f), new Vector2(73.55078f, 100.4805f), new Vector2(73.35156f, 100.4805f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(124.7695f, 95.32813f));
				pathBuilder.AddCubicBezier(new Vector2(124.6055f, 95.33203f), new Vector2(124.4531f, 95.25f), new Vector2(124.3633f, 95.11719f));
				pathBuilder.AddCubicBezier(new Vector2(123.3086f, 93.55078f), new Vector2(122.1055f, 92.08984f), new Vector2(120.7695f, 90.75781f));
				pathBuilder.AddCubicBezier(new Vector2(120.6406f, 90.63672f), new Vector2(120.5898f, 90.45703f), new Vector2(120.6367f, 90.28516f));
				pathBuilder.AddCubicBezier(new Vector2(120.6797f, 90.11328f), new Vector2(120.8125f, 89.98047f), new Vector2(120.9844f, 89.9375f));
				pathBuilder.AddCubicBezier(new Vector2(121.1563f, 89.89063f), new Vector2(121.3359f, 89.94141f), new Vector2(121.457f, 90.07031f));
				pathBuilder.AddCubicBezier(new Vector2(122.8398f, 91.44531f), new Vector2(124.082f, 92.95313f), new Vector2(125.168f, 94.57031f));
				pathBuilder.AddCubicBezier(new Vector2(125.2695f, 94.71875f), new Vector2(125.2813f, 94.91406f), new Vector2(125.1953f, 95.07422f));
				pathBuilder.AddCubicBezier(new Vector2(125.1133f, 95.23047f), new Vector2(124.9453f, 95.33203f), new Vector2(124.7656f, 95.33203f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(79.6875f, 90.96875f));
				pathBuilder.AddCubicBezier(new Vector2(79.48828f, 90.97266f), new Vector2(79.3125f, 90.85156f), new Vector2(79.23438f, 90.66797f));
				pathBuilder.AddCubicBezier(new Vector2(79.16016f, 90.48828f), new Vector2(79.20313f, 90.27734f), new Vector2(79.34375f, 90.13672f));
				pathBuilder.AddCubicBezier(new Vector2(80.71875f, 88.75781f), new Vector2(82.23047f, 87.51172f), new Vector2(83.84766f, 86.42578f));
				pathBuilder.AddCubicBezier(new Vector2(84.07031f, 86.27344f), new Vector2(84.375f, 86.33594f), new Vector2(84.52344f, 86.55859f));
				pathBuilder.AddCubicBezier(new Vector2(84.67578f, 86.78125f), new Vector2(84.61719f, 87.08594f), new Vector2(84.39063f, 87.23438f));
				pathBuilder.AddCubicBezier(new Vector2(82.82813f, 88.28516f), new Vector2(81.37109f, 89.48828f), new Vector2(80.03906f, 90.82031f));
				pathBuilder.AddCubicBezier(new Vector2(79.94922f, 90.91797f), new Vector2(79.82031f, 90.96875f), new Vector2(79.6875f, 90.96875f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(116.6602f, 87.25781f));
				pathBuilder.AddCubicBezier(new Vector2(116.5664f, 87.25781f), new Vector2(116.4727f, 87.23047f), new Vector2(116.3906f, 87.17969f));
				pathBuilder.AddCubicBezier(new Vector2(114.8242f, 86.13281f), new Vector2(113.1523f, 85.24219f), new Vector2(111.4102f, 84.52344f));
				pathBuilder.AddCubicBezier(new Vector2(111.1641f, 84.41406f), new Vector2(111.0508f, 84.13281f), new Vector2(111.1523f, 83.88672f));
				pathBuilder.AddCubicBezier(new Vector2(111.2539f, 83.64063f), new Vector2(111.5352f, 83.52344f), new Vector2(111.7813f, 83.61719f));
				pathBuilder.AddCubicBezier(new Vector2(113.5859f, 84.36328f), new Vector2(115.3125f, 85.28516f), new Vector2(116.9375f, 86.36719f));
				pathBuilder.AddCubicBezier(new Vector2(117.1133f, 86.48438f), new Vector2(117.1953f, 86.70703f), new Vector2(117.1328f, 86.91406f));
				pathBuilder.AddCubicBezier(new Vector2(117.0703f, 87.12109f), new Vector2(116.8789f, 87.26172f), new Vector2(116.6641f, 87.25781f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(89.18359f, 84.60156f));
				pathBuilder.AddCubicBezier(new Vector2(88.94922f, 84.60547f), new Vector2(88.74609f, 84.44141f), new Vector2(88.69531f, 84.21094f));
				pathBuilder.AddCubicBezier(new Vector2(88.64844f, 83.98438f), new Vector2(88.76953f, 83.75f), new Vector2(88.98828f, 83.66406f));
				pathBuilder.AddCubicBezier(new Vector2(90.78906f, 82.91406f), new Vector2(92.66016f, 82.34375f), new Vector2(94.57031f, 81.96094f));
				pathBuilder.AddCubicBezier(new Vector2(94.83594f, 81.90625f), new Vector2(95.09375f, 82.07813f), new Vector2(95.14844f, 82.33984f));
				pathBuilder.AddCubicBezier(new Vector2(95.20313f, 82.60547f), new Vector2(95.03125f, 82.86328f), new Vector2(94.76953f, 82.91797f));
				pathBuilder.AddCubicBezier(new Vector2(92.91797f, 83.28906f), new Vector2(91.10938f, 83.84375f), new Vector2(89.37109f, 84.57031f));
				pathBuilder.AddCubicBezier(new Vector2(89.3125f, 84.59375f), new Vector2(89.24609f, 84.60547f), new Vector2(89.18359f, 84.60156f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(106.1055f, 82.90625f));
				pathBuilder.AddCubicBezier(new Vector2(106.0742f, 82.91016f), new Vector2(106.043f, 82.91016f), new Vector2(106.0117f, 82.90625f));
				pathBuilder.AddCubicBezier(new Vector2(104.1758f, 82.55078f), new Vector2(102.3125f, 82.37109f), new Vector2(100.4414f, 82.37109f));
				pathBuilder.AddLine(new Vector2(100.3906f, 82.37109f));
				pathBuilder.AddCubicBezier(new Vector2(100.1211f, 82.37109f), new Vector2(99.90234f, 82.15234f), new Vector2(99.90234f, 81.88281f));
				pathBuilder.AddCubicBezier(new Vector2(99.90234f, 81.61328f), new Vector2(100.1211f, 81.39453f), new Vector2(100.3906f, 81.39453f));
				pathBuilder.AddLine(new Vector2(100.4375f, 81.39453f));
				pathBuilder.AddCubicBezier(new Vector2(102.3711f, 81.39453f), new Vector2(104.3008f, 81.58203f), new Vector2(106.1992f, 81.94922f));
				pathBuilder.AddCubicBezier(new Vector2(106.4688f, 81.97656f), new Vector2(106.668f, 82.21484f), new Vector2(106.6406f, 82.48828f));
				pathBuilder.AddCubicBezier(new Vector2(106.6133f, 82.75781f), new Vector2(106.375f, 82.95313f), new Vector2(106.1055f, 82.92578f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(106.1055f, 82.90625f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 9
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(55.51563f, 28.80859f));
				pathBuilder.AddLine(new Vector2(65.28125f, 28.80859f));
				pathBuilder.AddLine(new Vector2(65.28125f, 38.57422f));
				pathBuilder.AddLine(new Vector2(55.51563f, 38.57422f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(55.51563f, 28.80859f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 223, 237, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 10
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(65.28125f, 40.03906f));
				pathBuilder.AddLine(new Vector2(55.51563f, 40.03906f));
				pathBuilder.AddCubicBezier(new Vector2(54.70703f, 40.03906f), new Vector2(54.05469f, 39.38281f), new Vector2(54.05469f, 38.57422f));
				pathBuilder.AddLine(new Vector2(54.05469f, 28.80859f));
				pathBuilder.AddCubicBezier(new Vector2(54.05469f, 28f), new Vector2(54.70703f, 27.34375f), new Vector2(55.51563f, 27.34375f));
				pathBuilder.AddLine(new Vector2(65.28125f, 27.34375f));
				pathBuilder.AddCubicBezier(new Vector2(66.09375f, 27.34375f), new Vector2(66.75f, 28f), new Vector2(66.75f, 28.80859f));
				pathBuilder.AddLine(new Vector2(66.75f, 38.57422f));
				pathBuilder.AddCubicBezier(new Vector2(66.75f, 39.38281f), new Vector2(66.09375f, 40.03906f), new Vector2(65.28125f, 40.03906f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(56.98438f, 37.10938f));
				pathBuilder.AddLine(new Vector2(63.82031f, 37.10938f));
				pathBuilder.AddLine(new Vector2(63.82031f, 30.27344f));
				pathBuilder.AddLine(new Vector2(56.98438f, 30.27344f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(56.98438f, 37.10938f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 11
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(71.72656f, 28.80859f));
				pathBuilder.AddLine(new Vector2(81.49219f, 28.80859f));
				pathBuilder.AddLine(new Vector2(81.49219f, 38.57422f));
				pathBuilder.AddLine(new Vector2(71.72656f, 38.57422f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(71.72656f, 28.80859f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 223, 237, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 12
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(81.49219f, 40.03906f));
				pathBuilder.AddLine(new Vector2(71.72656f, 40.03906f));
				pathBuilder.AddCubicBezier(new Vector2(70.91797f, 40.03906f), new Vector2(70.26172f, 39.38281f), new Vector2(70.26172f, 38.57422f));
				pathBuilder.AddLine(new Vector2(70.26172f, 28.80859f));
				pathBuilder.AddCubicBezier(new Vector2(70.26172f, 28f), new Vector2(70.91797f, 27.34375f), new Vector2(71.72656f, 27.34375f));
				pathBuilder.AddLine(new Vector2(81.49219f, 27.34375f));
				pathBuilder.AddCubicBezier(new Vector2(82.30078f, 27.34375f), new Vector2(82.95703f, 28f), new Vector2(82.95703f, 28.80859f));
				pathBuilder.AddLine(new Vector2(82.95703f, 38.57422f));
				pathBuilder.AddCubicBezier(new Vector2(82.95703f, 39.38281f), new Vector2(82.30078f, 40.03906f), new Vector2(81.49219f, 40.03906f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(73.19141f, 37.10938f));
				pathBuilder.AddLine(new Vector2(80.02734f, 37.10938f));
				pathBuilder.AddLine(new Vector2(80.02734f, 30.27344f));
				pathBuilder.AddLine(new Vector2(73.19141f, 30.27344f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(73.19141f, 37.10938f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 13
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(87.93359f, 28.80859f));
				pathBuilder.AddLine(new Vector2(97.69922f, 28.80859f));
				pathBuilder.AddLine(new Vector2(97.69922f, 38.57422f));
				pathBuilder.AddLine(new Vector2(87.93359f, 38.57422f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(87.93359f, 28.80859f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 223, 237, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 14
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(97.69922f, 40.03906f));
				pathBuilder.AddLine(new Vector2(87.93359f, 40.03906f));
				pathBuilder.AddCubicBezier(new Vector2(87.125f, 40.03906f), new Vector2(86.46875f, 39.38281f), new Vector2(86.46875f, 38.57422f));
				pathBuilder.AddLine(new Vector2(86.46875f, 28.80859f));
				pathBuilder.AddCubicBezier(new Vector2(86.46875f, 28f), new Vector2(87.125f, 27.34375f), new Vector2(87.93359f, 27.34375f));
				pathBuilder.AddLine(new Vector2(97.69922f, 27.34375f));
				pathBuilder.AddCubicBezier(new Vector2(98.50781f, 27.34375f), new Vector2(99.16406f, 28f), new Vector2(99.16406f, 28.80859f));
				pathBuilder.AddLine(new Vector2(99.16406f, 38.57422f));
				pathBuilder.AddCubicBezier(new Vector2(99.16406f, 39.38281f), new Vector2(98.50781f, 40.03906f), new Vector2(97.69922f, 40.03906f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(89.39844f, 37.10938f));
				pathBuilder.AddLine(new Vector2(96.23438f, 37.10938f));
				pathBuilder.AddLine(new Vector2(96.23438f, 30.27344f));
				pathBuilder.AddLine(new Vector2(89.39844f, 30.27344f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(89.39844f, 37.10938f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 15
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(132.5078f, 33.69141f));
				pathBuilder.AddCubicBezier(new Vector2(132.5078f, 36.92578f), new Vector2(135.1328f, 39.55078f), new Vector2(138.3672f, 39.55078f));
				pathBuilder.AddCubicBezier(new Vector2(141.6016f, 39.55078f), new Vector2(144.2266f, 36.92578f), new Vector2(144.2266f, 33.69141f));
				pathBuilder.AddCubicBezier(new Vector2(144.2266f, 30.45703f), new Vector2(141.6016f, 27.83203f), new Vector2(138.3672f, 27.83203f));
				pathBuilder.AddCubicBezier(new Vector2(135.1328f, 27.83203f), new Vector2(132.5078f, 30.45703f), new Vector2(132.5078f, 33.69141f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(132.5078f, 33.69141f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 223, 237, 255));

				multiShapeVisual.Shapes.Add(shape);
			}

			// 16
			using (var pathBuilder = new CanvasPathBuilder(device))
			{
				pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Alternate);

				pathBuilder.BeginFigure(new Vector2(138.3672f, 41.01563f));
				pathBuilder.AddCubicBezier(new Vector2(134.3203f, 41.01563f), new Vector2(131.043f, 37.73828f), new Vector2(131.043f, 33.69141f));
				pathBuilder.AddCubicBezier(new Vector2(131.043f, 29.64453f), new Vector2(134.3203f, 26.36719f), new Vector2(138.3672f, 26.36719f));
				pathBuilder.AddCubicBezier(new Vector2(142.4141f, 26.36719f), new Vector2(145.6914f, 29.64453f), new Vector2(145.6914f, 33.69141f));
				pathBuilder.AddCubicBezier(new Vector2(145.6875f, 37.73438f), new Vector2(142.4102f, 41.01172f), new Vector2(138.3672f, 41.01563f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(138.3672f, 29.29688f));
				pathBuilder.AddCubicBezier(new Vector2(135.9414f, 29.29688f), new Vector2(133.9727f, 31.26563f), new Vector2(133.9727f, 33.69141f));
				pathBuilder.AddCubicBezier(new Vector2(133.9727f, 36.11719f), new Vector2(135.9414f, 38.08594f), new Vector2(138.3672f, 38.08594f));
				pathBuilder.AddCubicBezier(new Vector2(140.793f, 38.08594f), new Vector2(142.7617f, 36.11719f), new Vector2(142.7617f, 33.69141f));
				pathBuilder.AddCubicBezier(new Vector2(142.7578f, 31.26563f), new Vector2(140.793f, 29.30078f), new Vector2(138.3672f, 29.29688f));
				pathBuilder.EndFigure(CanvasFigureLoop.Closed);

				pathBuilder.BeginFigure(new Vector2(138.3672f, 29.29688f));
				pathBuilder.EndFigure(CanvasFigureLoop.Open);

				var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
				var geometry = compositor.CreatePathGeometry(path);
				var shape = compositor.CreateSpriteShape(geometry);

				shape.FillBrush = compositor.CreateColorBrush(new(255, 102, 169, 247));

				multiShapeVisual.Shapes.Add(shape);
			}

			multiShapeVisual.Size = new(200);
			ElementCompositionPreview.SetElementChildVisual(compPresenter3, multiShapeVisual);
		}
	}
}
