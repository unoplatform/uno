using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests
{
	[TestClass]
	public class Given_StaticResource
	{
		[TestMethod]
		public void When_xBind_Resource()
		{
			var SUT = new StaticResource_Control();

			Assert.IsNull(SUT.mySource.Source);

			SUT.MyProperty = 42;

			SUT.ForceLoaded();

			Assert.AreEqual(42, SUT.mySource.Source);
		}
	}
}
