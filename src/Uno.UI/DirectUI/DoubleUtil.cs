using System;
using System.Collections.Generic;
using System.Text;

namespace DirectUI
{
	internal class DoubleUtil
	{
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
