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
		public void When_AbsoluteLength()
		{
			var length = (GridLength)"4";
			Assert.IsTrue(length.IsAbsolute);
			Assert.IsFalse(length.IsStar);
			Assert.IsFalse(length.IsAuto);
			Assert.AreEqual(4, length.Value);
		}

		[TestMethod]
		public void When_LargeAbsoluteLength()
		{
			var length = (GridLength)"42";
			Assert.IsTrue(length.IsAbsolute);
			Assert.IsFalse(length.IsStar);
			Assert.IsFalse(length.IsAuto);
			Assert.AreEqual(42, length.Value);
		}

		[TestMethod]
		public void When_SmallStartLength()
		{
			var length = (GridLength)"4*";
			Assert.IsFalse(length.IsAbsolute);
			Assert.IsTrue(length.IsStar);
			Assert.IsFalse(length.IsAuto);
			Assert.AreEqual(4, length.Value);
		}

		[TestMethod]
		public void When_LargeStartLength()
		{
			var length = (GridLength)"42*";
			Assert.IsFalse(length.IsAbsolute);
			Assert.IsTrue(length.IsStar);
			Assert.IsFalse(length.IsAuto);
			Assert.AreEqual(42, length.Value);
		}

		[TestMethod]
		public void When_StartLength()
		{
			var length = (GridLength)"*";
			Assert.IsFalse(length.IsAbsolute);
			Assert.IsTrue(length.IsStar);
			Assert.IsFalse(length.IsAuto);
			Assert.AreEqual(1, length.Value);
		}

		[TestMethod]
		public void When_PaddedStartLength()
		{
			var length = (GridLength)" * ";
			Assert.IsFalse(length.IsAbsolute);
			Assert.IsTrue(length.IsStar);
			Assert.IsFalse(length.IsAuto);
			Assert.AreEqual(1, length.Value);
		}

		[TestMethod]
		public void When_AutoLength1()
		{
			var length = (GridLength)"auto";
			Assert.IsFalse(length.IsAbsolute);
			Assert.IsFalse(length.IsStar);
			Assert.IsTrue(length.IsAuto);
			Assert.AreEqual(0, length.Value);
		}

		[TestMethod]
		public void When_AutoLength2()
		{
			var length = (GridLength)"Auto";
			Assert.IsFalse(length.IsAbsolute);
			Assert.IsFalse(length.IsStar);
			Assert.IsTrue(length.IsAuto);
			Assert.AreEqual(0, length.Value);
		}
	}
}
