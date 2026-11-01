using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using System.Globalization;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class KeySpline : DependencyObject
	{
		private float _cp1X, _cp1Y, _cp2X, _cp2Y;
		private float _lastT;

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
				_cp1X = (float)_controlPoint1.X;
				_cp1Y = (float)_controlPoint1.Y;
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
				_cp2X = (float)_controlPoint2.X;
				_cp2Y = (float)_controlPoint2.Y;
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
			=> GetSplineProgress((float)linearProgress);

		// Copyright (c) Microsoft Corporation. All rights reserved.
		// Licensed under the MIT License.
		// MUX Reference KeySpline.cpp, tag winui3/release/2.5.1, commit ba3a8d59e
		internal float GetSplineProgress(float linearProgress)
		{
			if (linearProgress == 0 || linearProgress == 1)
			{
				return linearProgress;
			}

			// Dynamic search interval to clamp with
			float rBottom = 0.0f;
			float rTop = 1.0f;
			float rFuzz = 0.000001f; // computational zero
			float rAccuracy = 0.001f; // 1/3 the desired accuracy in X

			float rT = _lastT;
			while (rTop - rBottom > rFuzz)
			{
				GetXAndDeltaX(rT, out var rX, out var rDeltaX);

				var rAbsDeltaX = MathF.Abs(rDeltaX);

				// Clamp down the search interval, relying on the monotonicity of X(t)
				if (rX > linearProgress)
				{
					rTop = rT;
				}
				else
				{
					rBottom = rT;
				}

				// The desired accuracy is ultimately in y, not x; dy/dt <= 3 so we omit that factor
				if (MathF.Abs(rX - linearProgress) < rAccuracy * rAbsDeltaX)
				{
					break;
				}

				if (rAbsDeltaX > rFuzz)
				{
					// Nonzero derivative, use Newton-Raphson to obtain the next guess
					float rNext = rT - (rX - linearProgress) / rDeltaX;

					if (rNext >= rTop)
					{
						rT = (rT + rTop) / 2;
					}
					else if (rNext <= rBottom)
					{
						rT = (rT + rBottom) / 2;
					}
					else
					{
						rT = rNext;
					}
				}
				else
				{
					// Zero derivative, halve the search interval
					rT = (rBottom + rTop) / 2.0f;
				}
			}

			// Cache this value to reduce next iteration search
			_lastT = rT;

			return ComputeSplineValueY(rT);
		}

		private float ComputeSplineValueY(float rT)
		{
			float rOneMinusT = 1.0f - rT;
			float rOneMinusTSquared = rOneMinusT * rOneMinusT;
			float rTSquared = rT * rT;
			float rTCubed = rT * rTSquared;

			float rB13 = 3.0f * rOneMinusTSquared * rT;
			float rB23 = 3.0f * rOneMinusT * rTSquared;
			float rB33 = rTCubed;

			return rB13 * _cp1Y + rB23 * _cp2Y + rB33;
		}

		private void GetXAndDeltaX(float rT, out float rX, out float rDeltaX)
		{
			float rOneMinusT = 1.0f - rT;
			float rOneMinusTSquared = rOneMinusT * rOneMinusT;
			float rTSquared = rT * rT;
			float rTCubed = rT * rTSquared;

			float rB13 = 3.0f * rOneMinusTSquared * rT;
			float rB23 = 3.0f * rOneMinusT * rTSquared;
			float rB33 = rTCubed;

			rX = rB13 * _cp1X + rB23 * _cp2X + rB33;

			float rBX = 3.0f * _cp1X;
			float rCX = 3.0f * _cp2X;
			float rBX_CX = 2.0f * (rCX - rBX);
			float r3CX = 3.0f - rCX;

			rDeltaX = rOneMinusTSquared * rBX
				+ rT * rOneMinusT * rBX_CX
				+ r3CX * rTSquared;
		}
	}
}
