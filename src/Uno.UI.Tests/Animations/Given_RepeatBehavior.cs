using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Media.Animation;

namespace Uno.UI.Tests.Animations
{
	[TestClass]
	public class Given_RepeatBehavior
	{
		// note: Do not compare it against literal 'null'. Because we actually implemented an
		// implicit string-to-RepeatBehavior conversion on uno, which doesn't exist on windows.
		// Trying to compare that will result in a guaranteed FormatException.

		[TestMethod]
		public void When_RepeatBehavior_Is_Forever()
		{
			var behavior = new RepeatBehavior() { Type = RepeatBehaviorType.Forever };

			Assert.IsFalse(behavior.HasCount);
			Assert.IsFalse(behavior.HasDuration);

			Assert.IsTrue(behavior.Equals((object)RepeatBehavior.Forever));
			Assert.IsTrue(behavior.Equals(RepeatBehavior.Forever));
			Assert.IsTrue(RepeatBehavior.Equals(behavior, RepeatBehavior.Forever));

			Assert.IsFalse(behavior.Equals((object)new RepeatBehavior(3)));
			Assert.IsFalse(behavior.Equals(new RepeatBehavior(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(RepeatBehavior.Equals(behavior, new RepeatBehavior(8)));

			Assert.AreEqual("Forever", behavior.ToString());
		}


		[TestMethod]
		public void When_RepeatBehavior_Is_Count()
		{
			var behavior = new RepeatBehavior(4);

			Assert.IsTrue(behavior.HasCount);
			Assert.IsFalse(behavior.HasDuration);

			Assert.IsTrue(behavior.Equals((object)new RepeatBehavior(4)));
			Assert.IsTrue(behavior.Equals(new RepeatBehavior(4)));
			Assert.IsTrue(RepeatBehavior.Equals(behavior, new RepeatBehavior(4)));

			Assert.IsFalse(behavior.Equals((object)RepeatBehavior.Forever));
			Assert.IsFalse(behavior.Equals(new RepeatBehavior(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(RepeatBehavior.Equals(behavior, new RepeatBehavior(8)));

			Assert.AreEqual("4x", behavior.ToString());
		}


		[TestMethod]
		public void When_RepeatBehavior_Is_TimeSpan()
		{
			var behavior = new RepeatBehavior(TimeSpan.FromSeconds(24));

			Assert.IsFalse(behavior.HasCount);
			Assert.IsTrue(behavior.HasDuration);

			Assert.IsTrue(behavior.Equals((object)new RepeatBehavior(TimeSpan.FromSeconds(24))));
			Assert.IsTrue(behavior.Equals(new RepeatBehavior(TimeSpan.FromSeconds(24))));
			Assert.IsTrue(RepeatBehavior.Equals(behavior, new RepeatBehavior(TimeSpan.FromSeconds(24))));

			Assert.IsFalse(behavior.Equals((object)RepeatBehavior.Forever));
			Assert.IsFalse(behavior.Equals(new RepeatBehavior(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(RepeatBehavior.Equals(behavior, new RepeatBehavior(8)));

			Assert.AreEqual("00:00:24", behavior.ToString());
		}

		public static IEnumerable<object[]> GetParsingTestData()
		{
			object[] SafeGuard(string raw, RepeatBehavior expected) => new object[] { raw, expected };

			// <object property="iterationsx"/>
			yield return SafeGuard("0x", new RepeatBehavior(count: 0));
			yield return SafeGuard("0.5x", new RepeatBehavior(count: 0.5)); // nonsense but valid...

			// <object property="[days.]hours:minutes:seconds[.fractionalSeconds]"/>
			yield return SafeGuard("0:0:0.5", new RepeatBehavior(TimeSpan.FromSeconds(0.5)));
			yield return SafeGuard("1:2:3", new RepeatBehavior(new TimeSpan(1, 2, 3)));
			yield return SafeGuard("12:23:34", new RepeatBehavior(new TimeSpan(12, 23, 34)));
			yield return SafeGuard("1", new RepeatBehavior(TimeSpan.FromDays(1)));
			yield return SafeGuard("1.23:32:1", new RepeatBehavior(new TimeSpan(days: 1, 23, 32, 1)));
			yield return SafeGuard("1.2:3:4.5", new RepeatBehavior(new TimeSpan(days: 1, 2, 3, 4, milliseconds: 500))); // 5 is in fractional seconds

			// <object property="Forever"/>
			yield return SafeGuard("Forever", RepeatBehavior.Forever);
		}

		[DataTestMethod]
		[DynamicData(nameof(GetParsingTestData), DynamicDataSourceType.Method)]
		public void When_RepeatBehavior_From_String(string raw, RepeatBehavior expected)
		{
			Assert.AreEqual<RepeatBehavior>(expected, raw);
		}
	}
}
