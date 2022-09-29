#nullable disable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference DoubleUtil.cpp

using System;

namespace DirectUI
{
	internal class DoubleUtil
	{
		// Represents the smallest positive Double value that is greater than zero.
		// This is used to determine whether two doubles are effectively equal.  The
		// value is tailored to Silverlight which actually uses floats to represent
		// doubles in the core.
		public const double Epsilon = 1.1102230246251567e-016;

		// Represents positive infinity.
		public const double PositiveInfinity = double.PositiveInfinity;

		// Represents negative infinity.
		public const double NegativeInfinity = double.NegativeInfinity;

		// Represents a value that is not a number (NaN).
		public const double NaN = double.NaN; // This uses quiet_NaN in C++

		// Represents the maximum value.
		public const double MaxValue = double.MaxValue;

		// Represents the minimum value.
		public const double MinValue = double.MinValue;

		// Returns a value indicating whether the specified number evaluates
		// to negative or positive infinity.
		public static bool IsInfinity(
			double value)
		{
			return double.IsInfinity(value);
		}

		// Returns a value indicating whether the specified number evaluates
		// to positive infinity.
		public static bool IsPositiveInfinity(
			double value)
		{
			return double.IsPositiveInfinity(value);
		}

		// Returns a value indicating whether the specified number evaluates
		// to negative infinity.
		public static bool IsNegativeInfinity(
			double value)
		{
			return double.IsNegativeInfinity(value);
		}

		// Returns a value indicating whether the specified number evaluates
		// to a value that is not a number (NaN).
		public static bool IsNaN(
			double value)
		{
			return double.IsNaN(value);
		}

		// Return the absolute value of a number.
		public static double Abs(
			double value)
		{
			return (value < 0) ?
				-value :
				value;
		}

		// Returns the largest integer greater than or equal to the specified number.
		// Ceil(1.5) == 2
		// Ceil(-1.5) == -1
		public static double Ceil(
			double value)
		{
			return Math.Ceiling(value);
		}

		// Returns the largest integer less than or equal to the specified number.
		// Floor(1.5) == 1
		// Floor(-1.5) == -2
		public static double Floor(
			double value)
		{
			return Math.Floor(value);
		}

		// Returns fractional part of double
		public static double Fractional(
			double value)
		{
			return value > 0 ? (value - Math.Floor(value)) : (value - Math.Ceiling(value));
		}

		// Returns the larger of two specified numbers.
		public static double Max(
			double value1,
			double value2)
		{
			return (value1 >= value2) ?
				value1 :
				value2;
		}

		// Returns the smaller of two numbers.
		public static double Min(
			double value1,
			double value2)
		{
			return (value1 <= value2) ?
				value1 :
				value2;
		}

		// Returns whether or not two doubles are "close".  That is, whether or not they
		// are within epsilon of each other.  Note that this epsilon is proportional to
		// the numbers themselves so that AreClose survives scalar multiplication.
		// There are plenty of ways for this to return false even for numbers which are
		// theoretically identical, so no code calling this should fail to work if this
		// returns false.  This is important enough to repeat: NB:  NO CODE CALLING THIS
		// FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be used for
		// optimizations *only*.
		public static bool AreClose(
			double value1,
			double value2)

		{
			double epsilon = 0.0;
			double delta = 0.0;

			// If they're infinities, then the epsilon check doesn't work
			if (value1 == value2)
			{
				return true;
			}

			// This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < Epsilon
			epsilon = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * double.Epsilon;
			delta = value1 - value2;

			return (-epsilon < delta) && (epsilon > delta);
		}

		// Returns whether or not the first double is greater than the second double.
		// That is, whether or not the first is strictly greater than *and* not within
		// epsilon of the other number.  Note that this epsilon is proportional to the
		// numbers themselves to that AreClose survives scalar multiplication.  Note,
		// There are plenty of ways for this to return false even for numbers which are
		// theoretically identical, so no code calling this should fail to work if this
		// returns false.  This is important enough to repeat: NB: NO CODE CALLING THIS
		// FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be used for
		// optimizations *only*.
		public static bool GreaterThan(
			double value1,
			double value2)
		{
			return (value1 > value2) && !AreClose(value1, value2);
		}

		// Returns whether or not the first double is greater than or close to the
		// second double.  That is, whether or not the first is strictly greater than
		// or within epsilon of the other number.  Note that this epsilon is proportional
		// to the numbers themselves to that AreClose survives scalar multiplication.
		// Note, There are plenty of ways for this to return false even for numbers
		// which are theoretically identical, so no code calling this should fail to
		// work if this returns false.  This is important enough to repeat: NB: NO CODE
		// CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be used
		// for optimizations *only*.
		public static bool GreaterThanOrClose(
			double value1,
			double value2)
		{
			return (value1 > value2) || AreClose(value1, value2);
		}

		// Returns whether or not the double is "close" to 0.  Same as AreClose(double,
		// 0), but this is faster.
		public static bool IsZero(double value)
		{
			return Math.Abs(value) < 10.0 * double.Epsilon;
		}

		// Returns whether or not the first double is less than the second double.  That
		// is, whether or not the first is strictly less than *and* not within epsilon
		// of the other number.  Note that this epsilon is proportional to the numbers
		// themselves to that AreClose survives scalar multiplication.  Note, There are
		// plenty of ways for this to return false even for numbers which are
		// theoretically identical, so no code calling this should fail to work if this
		// returns false.  This is important enough to repeat: NB: NO CODE CALLING THIS
		// FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be used for
		// optimizations *only*.
		public static bool LessThan(
			double value1,
			double value2)
		{
			return (value1 < value2) && !AreClose(value1, value2);
		}

		// Returns whether or not the first double is less than or close to the second
		// double.  That is, whether or not the first is strictly less than or within
		// epsilon of the other number.  Note that this epsilon is proportional to the
		// numbers themselves to that AreClose survives scalar multiplication.  Note,
		// There are plenty of ways for this to return false even for numbers which are
		// theoretically identical, so no code calling this should fail to work if this
		// returns false.  This is important enough to repeat: NB: NO CODE CALLING THIS
		// FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be used for
		// optimizations *only*.
		public static bool LessThanOrClose(
			double value1,
			double value2)
		{
			return (value1 < value2) || AreClose(value1, value2);
		}

		// Rounds the fractional part of a double to the given number of decimal places.
		// If the digit found one past numDecimalPlaces is 5 or greater, then the digit at
		// numDecimalPlaces will be rounded up.  Otherwise, the value will simply be truncated
		// with numDecimalPlaces fractional digits.
		// NOTE: Does not handle the case where numDecimalPlaces causes us to go below
		// the min value of double.
		public static double Round(
			double originalValue,
			int numDecimalPlaces)
		{
			double scale = 1;

			for (int i = 0; i < numDecimalPlaces; i++)
				scale /= 10;

			return Floor(originalValue / scale + 0.5) * scale;
		}

		// Returns TRUE if a and b are within the given tolerance of each other.
		public static bool AreWithinTolerance(
			double a,
			double b,
			double tolerance)
		{
			return LessThanOrClose(Math.Abs(a - b), tolerance);
		}
	}
}
