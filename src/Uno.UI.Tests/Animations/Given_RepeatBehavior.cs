using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media.Animation;

namespace Uno.UI.Tests.Animations
{
	[TestClass]
	public class Given_RepeatBehavior
	{
		[TestMethod]
		public void When_RepeatBehavior_Is_Forever()
		{
			var behavior = new RepeatBehavior() { Type = RepeatBehaviorType.Forever };

			Assert.IsFalse(behavior.HasCount);
			Assert.IsFalse(behavior.HasDuration);

			Assert.IsTrue(behavior.Equals((object)RepeatBehavior.Forever));
			Assert.IsTrue(behavior.Equals(RepeatBehavior.Forever));
			Assert.IsTrue(RepeatBehavior.Equals(behavior, RepeatBehavior.Forever));

			Assert.IsFalse(behavior.Equals(null));
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

			Assert.IsFalse(behavior.Equals(null));
			Assert.IsFalse(behavior.Equals((object)RepeatBehavior.Forever));
			Assert.IsFalse(behavior.Equals(new RepeatBehavior(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(RepeatBehavior.Equals(behavior, new RepeatBehavior(8)));

			Assert.AreEqual("4", behavior.ToString());
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

			Assert.IsFalse(behavior.Equals(null));
			Assert.IsFalse(behavior.Equals((object)RepeatBehavior.Forever));
			Assert.IsFalse(behavior.Equals(new RepeatBehavior(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(RepeatBehavior.Equals(behavior, new RepeatBehavior(8)));

			Assert.AreEqual("00:00:24", behavior.ToString());
		}
	}
}
