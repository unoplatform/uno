using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;

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

		[TestMethod]
		[DataRow("00:00:05", "00:00:03", "00:00:08")]
		[DataRow("00:00:05", "Automatic", "Automatic")]
		[DataRow("00:00:05", "Forever", "Forever")]
		[DataRow("Automatic", "00:00:03", "Automatic")]
		[DataRow("Automatic", "Automatic", "Automatic")]
		[DataRow("Automatic", "Forever", "Automatic")]
		[DataRow("Forever", "00:00:03", "Forever")]
		[DataRow("Forever", "Automatic", "Automatic")]
		[DataRow("Forever", "Forever", "Forever")]
		public void When_Adding_Durations(string d1, string d2, string expected)
		{
			var duration1 = StringToDuration(d1);
			var duration2 = StringToDuration(d2);
			var expectedDuration = StringToDuration(expected);
			Assert.AreEqual(expectedDuration, duration1 + duration2);
			Assert.AreEqual(expectedDuration, duration1.Add(duration2));
			Assert.AreEqual(expectedDuration, duration2.Add(duration1));
		}

		[TestMethod]
		[DataRow("00:00:05", "00:00:03", "00:00:02")]
		[DataRow("00:00:05", "Automatic", "Automatic")]
		[DataRow("00:00:05", "Forever", "Automatic")]
		[DataRow("Automatic", "00:00:03", "Automatic")]
		[DataRow("Automatic", "Automatic", "Automatic")]
		[DataRow("Automatic", "Forever", "Automatic")]
		[DataRow("Forever", "00:00:03", "Forever")]
		[DataRow("Forever", "Automatic", "Automatic")]
		[DataRow("Forever", "Forever", "Automatic")]
		public void When_Subtracting_Durations(string d1, string d2, string expected)
		{
			var duration1 = StringToDuration(d1);
			var duration2 = StringToDuration(d2);
			var expectedDuration = StringToDuration(expected);
			Assert.AreEqual(expectedDuration, duration1 - duration2);
			Assert.AreEqual(expectedDuration, duration1.Subtract(duration2));
		}

		[TestMethod]
		[DataRow("00:00:05", "00:00:03", 1, true, true, false, false)]
		[DataRow("00:00:03", "00:00:05", -1, false, false, true, true)]
		[DataRow("00:00:03", "00:00:03", 0, false, true, false, true)]
		[DataRow("00:00:05", "Automatic", 1, false, false, false, false)]
		[DataRow("00:00:05", "Forever", -1, false, false, true, true)]
		[DataRow("Automatic", "00:00:03", -1, false, false, false, false)]
		[DataRow("Automatic", "Automatic", 0, false, true, false, true)]
		[DataRow("Automatic", "Forever", -1, false, false, false, false)]
		[DataRow("Forever", "00:00:03", 1, true, true, false, false)]
		[DataRow("Forever", "Automatic", 1, false, false, false, false)]
		[DataRow("Forever", "Forever", 0, false, true, false, true)]
		public void When_Comparing_Durations(string d1, string d2, int expectedCompare, bool isGreaterThan, bool isGreaterThanOrEquals, bool isLessThan, bool isLessThanOrEquals)
		{
			var duration1 = StringToDuration(d1);
			var duration2 = StringToDuration(d2);
			Assert.AreEqual(expectedCompare, duration1.CompareTo(duration2));
			Assert.AreEqual(expectedCompare, Duration.Compare(duration1, duration2));
			Assert.AreEqual(isGreaterThan, duration1 > duration2);
			Assert.AreEqual(isGreaterThanOrEquals, duration1 >= duration2);
			Assert.AreEqual(isLessThan, duration1 < duration2);
			Assert.AreEqual(isLessThanOrEquals, duration1 <= duration2);
		}

		private Duration StringToDuration(string s)
		{
			return s switch
			{
				"Automatic" => Duration.Automatic,
				"Forever" => Duration.Forever,
				_ => new Duration(TimeSpan.Parse(s)),
			};
		}
	}
}
