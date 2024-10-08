using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Markup;
using System.Numerics;
using Windows.Foundation;

#if __SKIA__
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_CompositionSpriteShape
{
#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Using_Path()
	{
		Vector2 scale = new(200f / 570f);
		Vector2 offset = -112.5f * scale;

		var expected = new ContentControl()
		{
			Width = 200,
			Height = 200,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Content = new Path
			{
				Margin = new(0, 0, -600, -600),
				Fill = new SolidColorBrush(Windows.UI.Colors.Black),
				Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry),
				"""
					M656.5 400.5C656.5 350.637 598.572 307.493 514.292 286.708C493.507 
					202.428 450.363 144.5 400.5 144.5C350.637 144.5 307.493 202.428 286.708 
					286.708C202.428 307.493 144.5 350.637 144.5 400.5C144.5 450.363 202.428 
					493.507 286.708 514.292C307.493 598.572 350.637 656.5 400.5 656.5C450.363 
					656.5 493.507 598.572 514.292 514.292C598.572 493.507 656.5 450.363 656.5 
					400.5ZM581.519 219.481C546.261 184.222 474.793 194.676 400.5 239.574C326.207 
					194.676 254.739 184.222 219.481 219.481C184.222 254.739 194.676 326.207 239.574 
					400.5C194.676 474.792 184.222 546.261 219.481 581.519C254.739 616.778 326.207 
					606.324 400.5 561.426C474.793 606.324 546.261 616.778 581.519 581.519C616.778 
					546.261 606.324 474.792 561.426 400.5C606.324 326.207 616.778 254.739 581.519 
					219.481ZM148.5 112.5L646.5 112.5L646.5 112.5Q647.384 112.5 648.266 112.543Q649.149 
					112.587 650.029 112.673Q650.908 112.76 651.782 112.89Q652.656 113.019 653.523 113.192Q654.39 
					113.364 655.247 113.579Q656.104 113.794 656.95 114.05Q657.796 114.307 658.628 114.604Q659.46 
					114.902 660.277 115.24Q661.093 115.579 661.892 115.956Q662.691 116.334 663.47 116.751Q664.25 
					117.167 665.008 117.622Q665.766 118.076 666.5 118.567Q667.235 119.058 667.945 119.585Q668.655 
					120.111 669.338 120.672Q670.021 121.232 670.676 121.826Q671.331 122.419 671.956 123.044Q672.581 
					123.669 673.174 124.324Q673.768 124.979 674.328 125.662Q674.889 126.345 675.415 127.055Q675.942 
					127.765 676.433 128.499Q676.924 129.234 677.378 129.992Q677.832 130.75 678.249 131.53Q678.666 132.309
					679.043 133.108Q679.421 133.907 679.76 134.723Q680.098 135.54 680.396 136.372Q680.693 137.204 
					680.95 138.05Q681.206 138.895 681.421 139.753Q681.636 140.61 681.808 141.477Q681.981 142.344 682.11 
					143.218Q682.24 144.092 682.327 144.971Q682.413 145.851 682.457 146.734Q682.5 147.616 682.5 148.5L682.5
					646.5L682.5 646.5Q682.5 647.384 682.457 648.266Q682.413 649.149 682.327 650.029Q682.24 650.908 682.11 
					651.782Q681.981 652.656 681.808 653.523Q681.636 654.39 681.421 655.247Q681.206 656.104 680.95 656.95Q680.693
					657.796 680.396 658.628Q680.098 659.46 679.76 660.277Q679.421 661.093 679.043 661.892Q678.666 662.691 678.249
					663.47Q677.832 664.25 677.378 665.008Q676.924 665.766 676.433 666.5Q675.942 667.235 675.415 667.945Q674.889
					668.655 674.328 669.338Q673.768 670.021 673.174 670.676Q672.581 671.331 671.956 671.956Q671.331 672.581
					670.676 673.174Q670.021 673.768 669.338 674.328Q668.655 674.889 667.945 675.415Q667.235 675.942 666.5 
					676.433Q665.766 676.924 665.008 677.378Q664.25 677.832 663.47 678.249Q662.691 678.666 661.892 679.043Q661.093
					679.421 660.277 679.76Q659.46 680.098 658.628 680.396Q657.796 680.693 656.95 680.95Q656.104 681.206 655.247 
					681.421Q654.39 681.636 653.523 681.808Q652.656 681.981 651.782 682.11Q650.908 682.24 650.029 682.327Q649.149 
					682.413 648.266 682.457Q647.384 682.5 646.5 682.5L148.5 682.5L148.5 682.5Q147.616 682.5 146.734 682.457Q145.851 
					682.413 144.971 682.327Q144.092 682.24 143.218 682.11Q142.344 681.981 141.477 681.808Q140.61 681.636 139.753 
					681.421Q138.895 681.206 138.05 680.95Q137.204 680.693 136.372 680.396Q135.54 680.098 134.723 679.76Q133.907 
					679.421 133.108 679.043Q132.309 678.666 131.53 678.249Q130.75 677.832 129.992 677.378Q129.234 676.924 128.499 
					676.433Q127.765 675.942 127.055 675.415Q126.345 674.889 125.662 674.328Q124.979 673.768 124.324 673.174Q123.669 
					672.581 123.044 671.956Q122.419 671.331 121.826 670.676Q121.232 670.021 120.672 669.338Q120.111 668.655 119.585 
					667.945Q119.058 667.235 118.567 666.5Q118.076 665.766 117.622 665.008Q117.167 664.25 116.751 663.47Q116.334 662.691 
					115.956 661.892Q115.579 661.093 115.24 660.277Q114.902 659.46 114.604 658.628Q114.307 657.796 114.05 656.95Q113.794 
					656.104 113.579 655.247Q113.364 654.39 113.192 653.523Q113.019 652.656 112.89 651.782Q112.76 650.908 112.673 
					650.029Q112.587 649.149 112.543 648.266Q112.5 647.384 112.5 646.5L112.5 148.5L112.5 148.5Q112.5 147.616 
					112.543 146.734Q112.587 145.851 112.673 144.971Q112.76 144.092 112.89 143.218Q113.019 142.344 113.192 
					141.477Q113.364 140.61 113.579 139.753Q113.794 138.895 114.05 138.05Q114.307 137.204 114.604 136.372Q114.902 
					135.54 115.24 134.723Q115.579 133.907 115.956 133.108Q116.334 132.309 116.751 131.53Q117.167 130.75 117.622 
					129.992Q118.076 129.234 118.567 128.499Q119.058 127.765 119.585 127.055Q120.111 126.345 120.672 125.662Q121.232 
					124.979 121.826 124.324Q122.419 123.669 123.044 123.044Q123.669 122.419 124.324 121.826Q124.979 121.232 125.662 
					120.672Q126.345 120.111 127.055 119.585Q127.765 119.058 128.499 118.567Q129.234 118.076 129.992 117.622Q130.75 
					117.167 131.53 116.751Q132.309 116.334 133.108 115.956Q133.907 115.579 134.723 115.24Q135.54 114.902 
					136.372 114.604Q137.204 114.307 138.05 114.05Q138.895 113.794 139.753 113.579Q140.61 113.364 141.477 
					113.192Q142.344 113.019 143.218 112.89Q144.092 112.76 144.971 112.673Q145.851 112.587 146.734 
					112.543Q147.616 112.5 148.5 112.5Z
				"""),
				RenderTransform = new CompositeTransform()
				{
					ScaleX = scale.X,
					ScaleY = scale.Y,
					TranslateX = offset.X,
					TranslateY = offset.Y
				}
			}
		};

		using (var builder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice()))
		{
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
			await RenderPath(path, expected, scale, offset);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Using_Circle()
	{
		var expected = new Ellipse()
		{
			Width = 200,
			Height = 200,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Fill = new SolidColorBrush(Windows.UI.Colors.Black)
		};

		await RenderPath(new CompositionPath(CanvasGeometry.CreateCircle(CanvasDevice.GetSharedDevice(), new(100, 100), 100)), expected);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Using_RoundedRectangle()
	{
		var expected = new Rectangle()
		{
			Width = 200,
			Height = 200,
			RadiusX = 16,
			RadiusY = 16,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Fill = new SolidColorBrush(Windows.UI.Colors.Black)
		};

		await RenderPath(new CompositionPath(CanvasGeometry.CreateRoundedRectangle(CanvasDevice.GetSharedDevice(), new(0, 0, 200, 200), 16, 16)), expected);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Using_Polygon()
	{
		var points = GetStarPoints(new(100), 100, 5, 0.5f);
		var pointCollection = new PointCollection();
		pointCollection.AddRange(points.Select(VectorExtensions.ToPoint));

		var expected = new Polygon()
		{
			Width = 200,
			Height = 200,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Fill = new SolidColorBrush(Windows.UI.Colors.Black),
			Points = pointCollection
		};

		await RenderPath(new CompositionPath(CanvasGeometry.CreatePolygon(CanvasDevice.GetSharedDevice(), points)), expected);
	}

	private async Task RenderPath(CompositionPath path, FrameworkElement expected, Vector2? scale = null, Vector2? offset = null, Windows.UI.Color? color = null)
	{
		var compositor = Compositor.GetSharedCompositor();
		var visual = compositor.CreateShapeVisual();
		var pathGeometry = compositor.CreatePathGeometry(path);
		var shape = compositor.CreateSpriteShape(pathGeometry);

		if (scale is not null)
		{
			shape.Scale = scale.Value;
		}

		if (offset is not null)
		{
			shape.Offset = offset.Value;
		}

		shape.FillBrush = compositor.CreateColorBrush(color ?? Windows.UI.Colors.Black);

		visual.Shapes.Add(shape);
		visual.Size = new((float)expected.Width, (float)expected.Height);

		var sut = new ContentControl
		{
			Width = expected.Width,
			Height = expected.Height
		};

		ElementCompositionPreview.SetElementChildVisual(sut, visual);

		var result = await Render(expected, sut);
		await ImageAssert.AreSimilarAsync(result.actual, result.expected);
	}

	private async Task<(RawBitmap expected, RawBitmap actual)> Render(FrameworkElement expected, FrameworkElement sut)
	{
		await UITestHelper.Load(new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(),
				new ColumnDefinition()
			},
			Children =
			{
				expected.Apply(e => Grid.SetColumn(e, 0)),
				sut.Apply(e => Grid.SetColumn(e, 1))
			},
			Height = 200,
			Width = 400
		});

		return (await UITestHelper.ScreenShot(expected), await UITestHelper.ScreenShot(sut));
	}

	private Vector2[] GetStarPoints(Vector2 center, float radius, int numPoints, float innerRadiusFactor)
	{
		Vector2[] points = new Vector2[numPoints * 2];
		float angleStep = 2f * MathF.PI / numPoints;
		float innerRadius = radius * innerRadiusFactor;

		for (int i = 0; i < numPoints * 2; i++)
		{
			float currentRadius = (i % 2 == 0) ? radius : innerRadius;
			float angle = i * angleStep / 2f - MathF.PI / 2f;

			float x = center.X + currentRadius * MathF.Cos(angle);
			float y = center.Y + currentRadius * MathF.Sin(angle);

			points[i] = new Vector2(x, y);
		}

		return points;
	}
#endif
}
