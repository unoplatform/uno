using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Animations
{
	[TestClass]
	public class Given_Duration
	{
		[TestMethod]
		public void When_Duration_Is_Automatic()
		{
			var duration = Duration.Automatic;

			Assert.IsFalse(duration.HasTimeSpan);

			Assert.IsTrue(duration.Equals((object)Duration.Automatic));
			Assert.IsTrue(duration.Equals(Duration.Automatic));
			Assert.IsTrue(Duration.Equals(duration, Duration.Automatic));

			Assert.IsFalse(duration.Equals(null));
			Assert.IsFalse(duration.Equals((object)Duration.Forever));
			Assert.IsFalse(duration.Equals(new Duration(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(Duration.Equals(duration, Duration.Forever));

			Assert.AreEqual("Automatic", duration.ToString());

			Assert.AreEqual(0, duration.CompareTo(Duration.Automatic));
			Assert.AreEqual(-1, duration.CompareTo(Duration.Forever));
			Assert.AreEqual(-1, Duration.Compare(duration, new Duration(TimeSpan.FromSeconds(24))));

			Assert.AreEqual(duration, duration.Add(new Duration(TimeSpan.FromSeconds(24))));
			Assert.AreEqual(duration, duration.Subtract(new Duration(TimeSpan.FromSeconds(24))));
		}

		[TestMethod]
		public void When_Duration_Is_Forever()
		{
			var duration = Duration.Forever;

			Assert.IsFalse(duration.HasTimeSpan);

			Assert.IsTrue(duration.Equals((object)Duration.Forever));
			Assert.IsTrue(duration.Equals(Duration.Forever));
			Assert.IsTrue(Duration.Equals(duration, Duration.Forever));

			Assert.IsFalse(duration.Equals(null));
			Assert.IsFalse(duration.Equals((object)Duration.Automatic));
			Assert.IsFalse(duration.Equals(new Duration(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(Duration.Equals(duration, Duration.Automatic));

			Assert.AreEqual("Forever", duration.ToString());

			Assert.AreEqual(1, duration.CompareTo(Duration.Automatic));
			Assert.AreEqual(0, duration.CompareTo(Duration.Forever));
			Assert.AreEqual(1, Duration.Compare(duration, new Duration(TimeSpan.FromSeconds(24))));

			Assert.AreEqual(duration, duration.Add(new Duration(TimeSpan.FromSeconds(24))));
			Assert.AreEqual(duration, duration.Subtract(new Duration(TimeSpan.FromSeconds(24))));
		}

		[TestMethod]
		public void When_Duration_Is_TimeSpan()
		{
			var duration = new Duration(TimeSpan.FromSeconds(24));

			Assert.IsTrue(duration.HasTimeSpan);

			Assert.IsTrue(duration.Equals((object)new Duration(TimeSpan.FromSeconds(24))));
			Assert.IsTrue(duration.Equals(new Duration(TimeSpan.FromSeconds(24))));
			Assert.IsTrue(Duration.Equals(duration, new Duration(TimeSpan.FromSeconds(24))));

			Assert.IsFalse(duration.Equals(null));
			Assert.IsFalse(duration.Equals((object)Duration.Automatic));
			Assert.IsFalse(duration.Equals(new Duration(TimeSpan.FromSeconds(30))));
			Assert.IsFalse(Duration.Equals(duration, Duration.Forever));

			Assert.AreEqual("00:00:24", duration.ToString());

			Assert.AreEqual(1, duration.CompareTo(Duration.Automatic));
			Assert.AreEqual(-1, duration.CompareTo(Duration.Forever));
			Assert.AreEqual(0, Duration.Compare(duration, new Duration(TimeSpan.FromSeconds(24))));

			Assert.AreEqual(new Duration(TimeSpan.FromSeconds(48)), duration.Add(new Duration(TimeSpan.FromSeconds(24))));
			Assert.AreEqual(new Duration(TimeSpan.FromSeconds(1)), duration.Subtract(new Duration(TimeSpan.FromSeconds(23))));
		}
	}
}
