using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public class Given_GridLength
	{
		[TestMethod]
		[DataRow("4", true, false, false, 4, null)]
		[DataRow("42", true, false, false, 42, null)]
		[DataRow("4*", false, true, false, 4, null)]
		[DataRow("*", false, true, false, 1, null)]
		[DataRow(" * ", false, true, false, 1, null)]
		[DataRow("auto", false, false, true, 1, null)]
		[DataRow(" auto", false, false, true, 1, null)]
		[DataRow(" Auto", false, false, true, 1, null)]
		[DataRow(" Auto ", false, false, true, 1, null)]
		[DataRow(" auto ", false, false, true, 1, null)]
		[DataRow(" auto ", false, false, true, 1, null)]
		[DataRow("*1", false, false, true, 0, typeof(InvalidOperationException))]
		[DataRow("Aauto", false, false, true, 0, typeof(InvalidOperationException))]
		[DataRow("abc", false, false, true, 0, typeof(InvalidOperationException))]
		[DataRow("42,5", false, false, true, 0, typeof(InvalidOperationException))]
		public void When_String(string value, bool isAbsolute, bool isStar, bool isAuto, double length, Type expectedException)
		{
			try
			{
				var gridLength = (GridLength)value;
				Assert.AreEqual(isAbsolute, gridLength.IsAbsolute);
				Assert.AreEqual(isStar, gridLength.IsStar);
				Assert.AreEqual(isAuto, gridLength.IsAuto);
				Assert.AreEqual(length, gridLength.Value);

				if (expectedException != null)
				{
					throw new InvalidOperationException($"Expected exception {expectedException}, got none");
				}
			}
			catch (Exception e)
			{
				if (e.GetType() != expectedException)
				{
					throw new InvalidOperationException($"Expected exception {e}, got {e}");
				}
			}

		}
	}
}
