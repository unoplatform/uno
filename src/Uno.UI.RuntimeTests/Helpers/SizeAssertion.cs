using System;
using Windows.Foundation;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Uno.UI
{
	internal static class SizeAssertionExtensions
	{
		internal static SizeAssertion Should(this Size size, double epsilon = 0.01d) => new SizeAssertion(size, epsilon);

		internal static PointAssertion Should(this Point point, double epsilon = 0.01d) => new PointAssertion(point, epsilon);

		internal static RectAssertion Should(this Rect rect, double epsilon = 0.01d) => new RectAssertion(rect, epsilon);

		internal static (bool isDifferent, double difference) CheckDifference(double value1, double value2, double epsilon)
		{
			if (value1.Equals(value2))
			{
				return (false, 0d);
			}

			var difference = Math.Abs(value1 - value2);
			return (difference > epsilon, difference);
		}
	}

	internal class SizeAssertion : ReferenceTypeAssertions<Size, SizeAssertion>
	{
		private readonly Size _size;
		private readonly double _epsilon;

		public SizeAssertion(Size size, in double epsilon)
		{
			_size = size;
			_epsilon = epsilon;
		}

		protected override string Identifier => "size";

		public AndConstraint<SizeAssertion> Be(Size expectedSize, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			using (new AssertionScope(_size.ToString()))
			{
				_size.Should().BeOfWidth(expectedSize.Width, epsilon, because, becauseArgs)
					.And.BeOfHeight(expectedSize.Height, epsilon, because, becauseArgs);
			}

			return new AndConstraint<SizeAssertion>(this);
		}

		public AndConstraint<SizeAssertion> BeOfWidth(double expectedWidth, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			Validate(expectedWidth, _size.Width, epsilon, because, becauseArgs, "Width");

			return new AndConstraint<SizeAssertion>(this);
		}

		public AndConstraint<SizeAssertion> BeOfHeight(double expectedHeight, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			Validate(expectedHeight, _size.Height, epsilon, because, becauseArgs, "Height");

			return new AndConstraint<SizeAssertion>(this);
		}

		private void Validate(double expected, double value, double? epsilon, string because, object[] becauseArgs,  string field)
		{
			var ep = epsilon ?? _epsilon;
			var d = SizeAssertionExtensions.CheckDifference(expected, value, ep);
			Execute.Assertion
				.BecauseOf(because, becauseArgs)
				.ForCondition(!d.isDifferent)
				.FailWith($"Expected {field} of {_size}{{reason}} to be {expected}, but seems to be {value} (difference of {d.difference}) with a tolerance of {ep}.");
		}
	}

	internal class PointAssertion : ReferenceTypeAssertions<Size, SizeAssertion>
	{
		private readonly Point _point;
		private readonly double _epsilon;

		public PointAssertion(Point point, in double epsilon)
		{
			_point = point;
			_epsilon = epsilon;
		}

		protected override string Identifier => "point";

		public AndConstraint<PointAssertion> Be(Point expectedPoint, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			using (new AssertionScope(_point.ToString()))
			{
				_point.Should().BeAtX(expectedPoint.X, epsilon, because, becauseArgs)
					.And.BeAtY(expectedPoint.Y, epsilon, because, becauseArgs);
			}

			return new AndConstraint<PointAssertion>(this);
		}

		private void Validate(double expected, double value, double? epsilon, string because, object[] becauseArgs,
			string field)
		{
			var ep = epsilon ?? _epsilon;
			var d = SizeAssertionExtensions.CheckDifference(expected, value, ep);
			Execute.Assertion
				.BecauseOf(because, becauseArgs)
				.ForCondition(!d.isDifferent)
				.FailWith($"Expected {field} of {_point}{{reason}} to be at {expected}, but seems to be at {value} (difference of {d.difference}) with a tolerance of {ep}.");
		}

		public AndConstraint<PointAssertion> BeAtX(double expectedX, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			Validate(expectedX, _point.X, epsilon, because, becauseArgs, "X");

			return new AndConstraint<PointAssertion>(this);
		}

		public AndConstraint<PointAssertion> BeAtY(double expectedY, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			Validate(expectedY, _point.Y, epsilon, because, becauseArgs, "Y");

			return new AndConstraint<PointAssertion>(this);
		}
	}

	internal class RectAssertion : ReferenceTypeAssertions<Size, SizeAssertion>
	{
		private readonly Rect _rect;
		private readonly double _epsilon;

		public RectAssertion(Rect rect, in double epsilon)
		{
			_rect = rect;
			_epsilon = epsilon;
		}

		protected override string Identifier => "rect";

		public AndConstraint<RectAssertion> Be(Rect expectedRect, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			using (new AssertionScope(_rect.ToString()))
			{
				new Size(_rect.Width, _rect.Height).Should(_epsilon).Be(new Size(expectedRect.Width, expectedRect.Height), epsilon, because, becauseArgs);
				new Point(_rect.X, _rect.Y).Should(_epsilon).Be(new Point(expectedRect.X, expectedRect.Y), epsilon, because, becauseArgs);
			}

			return new AndConstraint<RectAssertion>(this);
		}

		public AndConstraint<RectAssertion> HaveSize(Size expectedSize, double? epsilon = null, string because = null,  params object[] becauseArgs)
		{
			new Size(_rect.Width, _rect.Height).Should(_epsilon).Be(expectedSize, epsilon, because, becauseArgs);

			return new AndConstraint<RectAssertion>(this);
		}

		public AndConstraint<RectAssertion> BeAt(Point expectedPoint, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			new Point(_rect.X, _rect.Y).Should(_epsilon).Be(expectedPoint, epsilon, because, becauseArgs);

			return new AndConstraint<RectAssertion>(this);
		}
	}
}
