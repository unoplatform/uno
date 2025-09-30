using System;
using Windows.Foundation;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace Uno.UI
{
	internal static class SizeAssertionExtensions
	{
		internal static SizeAssertion Should(this Size size, double epsilon = 0.01d)
			=> new SizeAssertion(size, epsilon, AssertionChain.GetOrCreate());

		internal static PointAssertion Should(this Point point, double epsilon = 0.01d)
			=> new PointAssertion(point, epsilon, AssertionChain.GetOrCreate());

		internal static RectAssertion Should(this Rect rect, double epsilon = 0.01d)
			=> new RectAssertion(rect, epsilon, AssertionChain.GetOrCreate());

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

		public SizeAssertion(Size size, in double epsilon, AssertionChain assertionChain)
			: base(size, assertionChain)
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

		private void Validate(double expected, double value, double? epsilon, string because, object[] becauseArgs, string field)
		{
			var ep = epsilon ?? _epsilon;
			var d = SizeAssertionExtensions.CheckDifference(expected, value, ep);
			CurrentAssertionChain
				.BecauseOf(because, becauseArgs)
				.ForCondition(!d.isDifferent)
				.FailWith($"Expected {field} of {_size}{{reason}} to be {expected}, but seems to be {value} (difference of {d.difference}) with a tolerance of {ep}.");
		}
	}

	internal class PointAssertion : ReferenceTypeAssertions<Point, PointAssertion>
	{
		private readonly Point _point;
		private readonly double _epsilon;

		public PointAssertion(Point point, in double epsilon, AssertionChain assertionChain)
			: base(point, assertionChain)
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
			CurrentAssertionChain
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

	internal class RectAssertion : ReferenceTypeAssertions<Rect, RectAssertion>
	{
		private readonly Rect _rect;
		private readonly double _epsilon;

		public RectAssertion(Rect rect, in double epsilon, AssertionChain assertionChain)
			: base(rect, assertionChain)
		{
			_rect = rect;
			_epsilon = epsilon;
		}

		protected override string Identifier => "rect";

		public AndConstraint<RectAssertion> Be(Rect expectedRect, double? epsilon = null, string because = null, params object[] becauseArgs)
		{
			using (new AssertionScope(AssertionScope.Current?.Name() + _rect.ToString()))
			{
				// Note: We do NOT compare Size and Point, as Rect.Empty will throw in Size ctor on UWP.

				if (double.IsInfinity(expectedRect.X))
				{
					_rect.X.Should().Be(expectedRect.X, because + " (X)", becauseArgs);
				}
				else
				{
					_rect.X.Should().BeApproximately(expectedRect.X, epsilon ?? _epsilon, because + " (X)", becauseArgs);
				}

				if (double.IsInfinity(expectedRect.Y))
				{
					_rect.Y.Should().Be(expectedRect.Y, because + " (Y)", becauseArgs);
				}
				else
				{
					_rect.Y.Should().BeApproximately(expectedRect.Y, epsilon ?? _epsilon, because + " (Y)", becauseArgs);
				}

				if (double.IsInfinity(expectedRect.Width))
				{
					_rect.Width.Should().Be(expectedRect.Width, because + " (width)", becauseArgs);
				}
				else
				{
					_rect.Width.Should().BeApproximately(expectedRect.Width, epsilon ?? _epsilon, because + " (width)", becauseArgs);
				}

				if (double.IsInfinity(expectedRect.Height))
				{
					_rect.Height.Should().Be(expectedRect.Height, because + " (height)", becauseArgs);
				}
				else
				{
					_rect.Height.Should().BeApproximately(expectedRect.Height, epsilon ?? _epsilon, because + " (height)", becauseArgs);
				}
			}

			return new AndConstraint<RectAssertion>(this);
		}

		public AndConstraint<RectAssertion> HaveSize(Size expectedSize, double? epsilon = null, string because = null, params object[] becauseArgs)
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
