using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using System.Globalization;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class KeySpline : DependencyObject
	{
		const int Steps = 64;

		private Point[] _positions;
		private bool _isDirty;

		/// <summary>
		/// Initializes a new instance of the KeySpline class.
		/// </summary>
		public KeySpline()
		{
			InitializeBinder();

			ControlPoint1 = new Point(0, 0);
			ControlPoint2 = new Point(1, 1);
		}

		/// <summary>
		/// Initializes a new instance of the KeySpline class with the specified coordinates for the control points.
		/// </summary>
		/// <param name="x1">The x-coordinate for the ControlPoint1 of the KeySpline.</param>
		/// <param name="y1">The y-coordinate for the ControlPoint1 of the KeySpline.</param>
		/// <param name="x2">The x-coordinate for the ControlPoint2 of the KeySpline.</param>
		/// <param name="y2">The y-coordinate for the ControlPoint2 of the KeySpline.</param>
		public KeySpline(double x1, double y1, double x2, double y2)
		{
			ControlPoint1 = new Point(x1, y1);
			ControlPoint2 = new Point(x2, y2);
		}

		/// <summary>
		/// Initializes a new instance of the KeySpline class with the specified control points.
		/// </summary>
		/// <param name="controlPoint1">The control point for the ControlPoint1 of the KeySpline.</param>
		/// <param name="controlPoint2">The control point for the ControlPoint2 of the KeySpline.</param>
		public KeySpline(Point controlPoint1, Point controlPoint2)
		{
			ControlPoint1 = controlPoint1;
			ControlPoint2 = controlPoint2;
		}

		private Point _controlPoint1;

		/// <summary>
		/// The Bezier curve's first control point. The point's X and Y values must each be between 0 and 1, inclusive. The default value is (0,0).
		/// </summary>
		public Point ControlPoint1
		{
			get => _controlPoint1;
			set
			{
				_controlPoint1 = ValidateControlPointValue(value);
				_isDirty = true;
			}
		}

		private Point _controlPoint2;

		/// <summary>
		/// The Bezier curve's second control point. The point's X and Y values must each be between 0 and 1, inclusive. The default value is (1,1).
		/// </summary>
		public Point ControlPoint2
		{
			get => _controlPoint2;
			set
			{
				_controlPoint2 = ValidateControlPointValue(value);
				_isDirty = true;
			}
		}

		public override string ToString()
			=> "CP1: {0}, CP2: {1}".InvariantCultureFormat(ControlPoint1, ControlPoint2);

		private static Point ValidateControlPointValue(Point controlPointValue)
		{
			if (controlPointValue.X > 1 ||
				controlPointValue.X < 0 ||
				controlPointValue.Y > 1 ||
				controlPointValue.Y < 0)
			{
				throw new ArgumentOutOfRangeException("A control point's coordinates must be between 0 and 1, inclusive");
			}

			return controlPointValue;
		}

		public static KeySpline FromString(string input)
		{
			var tokens = input
				.Split(',', ' ')
				.Where(t => !t.IsNullOrEmpty())
				.ToList();

			if (tokens.Count != 4)
			{
				throw new ArgumentOutOfRangeException("A KeySpline must have 4 tokens: x1, y1, x2, y2. Yours had {0} (input: \"{1}\").".InvariantCultureFormat(tokens.Count, input));
			}

			return new KeySpline(
				double.Parse(tokens[0], CultureInfo.InvariantCulture),
				double.Parse(tokens[1], CultureInfo.InvariantCulture),
				double.Parse(tokens[2], CultureInfo.InvariantCulture),
				double.Parse(tokens[3], CultureInfo.InvariantCulture));
		}

		public static implicit operator KeySpline(string input)
			=> FromString(input);

		/// <summary>
		/// Calculates spline progress from a linear progress.
		/// </summary>
		/// <param name="linearProgress">the linear progress</param>
		/// <returns>the spline progress</returns>
		public double GetSplineProgress(double linearProgress)
		{
			Build();

			for (int i = _positions.Length - 2; i >= 0; i--)
			{
				var currentPos = _positions[i];

				if (linearProgress >= currentPos.X)
				{
					var nextPos = _positions[i + 1];

					var deltaX = nextPos.X - currentPos.X;
					var deltaY = nextPos.Y - currentPos.Y;

					var innerLinearProgress = (linearProgress - currentPos.X) / deltaX;

					var ret = currentPos.Y + deltaY * innerLinearProgress;

					return ret;
				}
			}

			return -1;
		}


		private Point GetProgress(float t)
		{
			var bx = ControlPoint1.X;
			var by = ControlPoint1.Y;
			var cx = ControlPoint2.X;
			var cy = ControlPoint2.Y;

			var B1_t = 3 * t * Math.Pow(1 - t, 2);
			var B2_t = 3 * Math.Pow(t, 2) * (1 - t);
			var B3_t = Math.Pow(t, 3);

			var px_t = (B1_t * bx) + (B2_t * cx) + B3_t;
			var py_t = (B1_t * by) + (B2_t * cy) + B3_t;

			return new Point(px_t, py_t);
		}

		private void Build()
		{
			if (_isDirty)
			{
				_isDirty = false;

				_positions = new Point[Steps + 1];

				for (int i = 0; i < Steps + 1; i++)
				{
					_positions[i] = GetProgress(i / (float)Steps);
				}
			}
		}
	}
}
