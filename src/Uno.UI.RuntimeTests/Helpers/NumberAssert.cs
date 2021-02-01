using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Helpers
{
	internal static class NumberAssert
	{
		public static void Greater(double arg1, double arg2)
		{
			var isGreater = arg1 > arg2;
			if (!isGreater)
			{
				throw new AssertFailedException($"{nameof(arg1)}={arg1} was expected to be greater than {nameof(arg2)}={arg2}");
			}
		}

		public static void Less(double arg1, double arg2)
		{
			var isLess = arg1 < arg2;
			if (!isLess)
			{
				throw new AssertFailedException($"{nameof(arg1)}={arg1} was expected to be less than {nameof(arg2)}={arg2}");
			}
		}
	}
}
