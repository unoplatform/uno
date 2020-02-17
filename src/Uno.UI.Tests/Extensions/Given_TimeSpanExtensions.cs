using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;

namespace Uno.UI.Tests.Extensions
{
	[TestClass]
	public class Given_TimeSpanExtensions
	{
		[DataTestMethod]
		[DataRow(0, -5, -30)]
		[DataRow(-1, -5, -30)]
		[DataRow(-3, -5, -30)]
		[DataRow(-8, -5, -30)]
		public void When_Calling_NormalizeToDay_On_Negative_Then_Normalized(int days, int hours, int minutes)
		{
			var input = new TimeSpan(days, hours, minutes, seconds: 0);

			var expected = new TimeSpan(days: 0, hours: 18, minutes: 30, seconds: 0);
			var actual = input.NormalizeToDay();

			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(0, 0, 0)]
		[DataRow(0, 9, 30)]
		[DataRow(0, 20, 30)]
		[DataRow(1, 0, 0)]
		public void When_Calling_NormalizeToDay_On_Normal_Then_Equal(int days, int hours, int minutes)
		{
			var input = new TimeSpan(days, hours, minutes, seconds: 0);

			var expected = input;
			var actual = input.NormalizeToDay();

			Assert.AreEqual(expected, actual);
		}

		[DataTestMethod]
		[DataRow(1, 5, 30)]
		[DataRow(3, 5, 30)]
		[DataRow(8, 5, 30)]
		public void When_Calling_NormalizeToDay_On_Excessive_Then_Normalized(int days, int hours, int minutes)
		{
			var input = new TimeSpan(days, hours, minutes, seconds: 0);

			var expected = new TimeSpan(days: 0, hours: 5, minutes: 30, seconds: 0);
			var actual = input.NormalizeToDay();

			Assert.AreEqual(expected, actual);
		}
	}
}
