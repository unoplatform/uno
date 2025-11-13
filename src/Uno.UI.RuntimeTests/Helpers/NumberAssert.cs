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
		/// <summary>
		/// Asserts that <paramref name="arg1"/> is greater than <paramref name="arg2"/>.
		/// </summary>
		public static void Greater(double arg1, double arg2)
		{
			var isGreater = arg1 > arg2;
			if (!isGreater)
			{
				throw new AssertFailedException($"{nameof(arg1)}={arg1} was expected to be greater than {nameof(arg2)}={arg2}");
			}
		}
		/// <summary>
		/// Asserts that <paramref name="arg1"/> is greater than or equal to <paramref name="arg2"/>.
		/// </summary>
		public static void GreaterOrEqual(double arg1, double arg2)
		{
			var isGreater = arg1 >= arg2;
			if (!isGreater)
			{
				throw new AssertFailedException($"{nameof(arg1)}={arg1} was expected to be greater than or equal to {nameof(arg2)}={arg2}");
			}
		}

		/// <summary>
		/// Asserts that <paramref name="arg1"/> is less than <paramref name="arg2"/>.
		/// </summary>
		public static void Less(double arg1, double arg2)
		{
			var isLess = arg1 < arg2;
			if (!isLess)
			{
				throw new AssertFailedException($"{nameof(arg1)}={arg1} was expected to be less than {nameof(arg2)}={arg2}");
			}
		}

		/// <summary>
		/// Asserts that <paramref name="arg1"/> is less than or equal to <paramref name="arg2"/>.
		/// </summary>
		public static void LessOrEqual(double arg1, double arg2)
		{
			var isLess = arg1 <= arg2;
			if (!isLess)
			{
				throw new AssertFailedException($"{nameof(arg1)}={arg1} was expected to be less than or equal to {nameof(arg2)}={arg2}");
			}
		}
	}

	public static class AssertionExtensions
	{
		public static AndConstraint<AwesomeAssertions.Collections.GenericCollectionAssertions<float>> BeEquivalentToWithTolerance<T>(this AwesomeAssertions.Collections.GenericCollectionAssertions<float> assertions, IEnumerable<float> expected, float tolerance)
		{
			assertions.Subject.Should().HaveCount(expected.Count());

			var subject = assertions.Subject.ToArray();
			using var enumerator = expected.GetEnumerator();
			enumerator.MoveNext();

			foreach (var actual in subject)
			{
				var expectedValue = enumerator.Current;
				enumerator.MoveNext();
				actual.Should().BeApproximately(expectedValue, tolerance);
			}

			return new AndConstraint<AwesomeAssertions.Collections.GenericCollectionAssertions<float>>(assertions);
		}
	}
}
