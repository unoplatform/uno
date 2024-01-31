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
		}
	}
}
