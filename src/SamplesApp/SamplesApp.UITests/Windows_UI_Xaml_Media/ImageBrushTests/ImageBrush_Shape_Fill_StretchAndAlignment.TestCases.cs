using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.ImageBrushTests
{
	partial class ImageBrush_Shape_Fill_StretchAndAlignment
	{
		private static ExpectedColor[] GetExpectedColors()
		{
			return new[]
			{
				// X: Center Y: Center
				// W: 100 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Center Y: Center
				// W: 50 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { red, white, white, white, red, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { red, white, white, white, red, white, white, white, red }
				},

				// X: Center Y: Center
				// W: 100 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, red, white, white, white, red, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, white, green, green, green, white, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, red, white, white, white, red, white, red }
				},

				// X: Center Y: Center
				// W: 50 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { red, red, red, red, red, red, red, red, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Center Y: Center
				// W: 200 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, green, green, green, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Center Y: Center
				// W: 150 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, green, green, green, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Center Y: Center
				// W: 200 H: 150
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, green, green, green, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, white, green, green, green, white, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Center
				// W: 100 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Center
				// W: 50 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, red, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, red, white, white, white, red }
				},

				// X: Left Y: Center
				// W: 100 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, red, white, white, white, red, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, green, green, green, green, green, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, red, white, white, white, red, white, red }
				},

				// X: Left Y: Center
				// W: 50 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, red, red, red, red, red, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Center
				// W: 200 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, green, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Center
				// W: 150 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, green, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, red, white, white, white, red }
				},

				// X: Left Y: Center
				// W: 200 H: 150
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, green, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, green, green, green, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Top
				// W: 100 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Top
				// W: 50 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, red, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, white, white, white, green, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, red, white, white, white, red }
				},

				// X: Left Y: Top
				// W: 100 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, red, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, green, green, green, green, green, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, red, white, red }
				},

				// X: Left Y: Top
				// W: 50 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, red, red, red, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Top
				// W: 200 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, white, green, green, green, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Top
				// W: 150 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, white, white, green, green, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, red, white, white, white, red }
				},

				// X: Left Y: Top
				// W: 200 H: 150
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, green, green, green, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, green, green, green, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, red, white, red }
				},

				// X: Left Y: Bottom
				// W: 100 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Bottom
				// W: 50 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, red, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, green, green, white, white, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, red, white, white, white, red }
				},

				// X: Left Y: Bottom
				// W: 100 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, red, white, white, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, green, green, green, green, green, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, red, white, white, white, white, white, red }
				},

				// X: Left Y: Bottom
				// W: 50 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, red, red, red, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Bottom
				// W: 200 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, green, green, green, green, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Left Y: Bottom
				// W: 150 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, green, green, green, white, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, green, green, green, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, red, white, white, white, red }
				},

				// X: Left Y: Bottom
				// W: 200 H: 150
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, green, green, green, green, green, green, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, green, green, green, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, red, white, white, white, white, white, red }
				},

				// X: Right Y: Center
				// W: 100 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Center
				// W: 50 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { red, white, white, white, white, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { red, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Center
				// W: 100 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, red, white, white, white, red, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, green, white, white, white, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, red, white, white, white, red, white, red }
				},

				// X: Right Y: Center
				// W: 50 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { red, red, red, white, white, white, red, red, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Center
				// W: 200 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, green, green, white, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Center
				// W: 150 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, green, green, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, green, green, green, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { red, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Center
				// W: 200 H: 150
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, green, green, white, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { green, green, white, white, white, white, white, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Center,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Top
				// W: 100 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Top
				// W: 50 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { red, white, white, white, white, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, white, white, white, green, green, green, green, white }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { red, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Top
				// W: 100 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, red, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, green, green, white, white, white, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, red, white, red }
				},

				// X: Right Y: Top
				// W: 50 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { red, white, white, white, white, white, red, red, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Top
				// W: 200 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, green, green, white, white, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Top
				// W: 150 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, green, white, white, green, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, green, green, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { red, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Top
				// W: 200 H: 150
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, green, green, white, white, green, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { green, green, white, white, white, white, white, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Top,
					Colors = new [] { white, white, white, white, white, white, red, white, red }
				},

				// X: Right Y: Bottom
				// W: 100 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Bottom
				// W: 50 H: 100
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { red, white, white, white, white, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, green, green, white, white, white, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 100),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { red, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Bottom
				// W: 100 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, red, white, white, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, white, white, white, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(100, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, red, white, white, white, white, white, red }
				},

				// X: Right Y: Bottom
				// W: 50 H: 50
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { red, red, red, white, white, white, white, white, red }

				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(50, 50),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Bottom
				// W: 200 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, green, green, white, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Bottom
				// W: 150 H: 200
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, green, green, white, white, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, green, green, green, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(150, 200),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { red, white, white, white, white, white, white, white, red }
				},

				// X: Right Y: Bottom
				// W: 200 H: 150
				new ExpectedColor {
					Stretch = Stretch.None,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, green, green, white, white, green, green, green }
				},
				new ExpectedColor {
					Stretch = Stretch.Fill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, white, white, white, white, white, white, red }
				},
				new ExpectedColor {
					Stretch = Stretch.Uniform,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { green, green, white, white, white, white, white, green, red }
				},
				new ExpectedColor {
					Stretch = Stretch.UniformToFill,
					Size = new Size(200, 150),
					AlignmentX = AlignmentX.Right,
					AlignmentY = AlignmentY.Bottom,
					Colors = new [] { white, white, red, white, white, white, white, white, red }
				},
			};
		}
	}
}
