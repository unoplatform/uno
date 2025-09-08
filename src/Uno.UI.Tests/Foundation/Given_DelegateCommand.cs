using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Uno.UI.Common;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_DelegateCommand
	{
		[TestMethod]
		public void When_Correct()
		{
			var target = 0;
			var cmd = new DelegateCommand<int>(i => target = i);

			Assert.AreEqual(0, target);
			cmd.Execute(10);
			Assert.AreEqual(10, target);
		}

		[TestMethod]
		public void When_Parameter_Is_Wrong_Type()
		{
			var target = 0;
			var cmd = new DelegateCommand<int>(i => target = i);

			Assert.AreEqual(0, target);
			Assert.ThrowsExactly<InvalidCastException>(() => cmd.Execute("10"));
		}

		[TestMethod]
		public void When_Parameter_Is_Null_And_Valid()
		{
			var target = new object();
			var cmd = new DelegateCommand<object>(i => target = i);

			Assert.IsNotNull(target);
			cmd.Execute(null);
			Assert.IsNull(target);
		}

		[TestMethod]
		public void When_Parameter_Is_Null_And_Invalid()
		{
			var target = 0;
			var cmd = new DelegateCommand<int>(i => target = i);

			Assert.AreEqual(0, target);
			Assert.ThrowsExactly<InvalidCastException>(() => cmd.Execute(null));
		}
	}
}
