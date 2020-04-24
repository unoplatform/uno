using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_CustomResource
	{
		[TestInitialize]
		public void Initialize()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_Assigned_To_Property()
		{
			var page = new Test_Page_Other();
			Assert.AreEqual("Map of the victories I win", page.customResourceTextBlock.Text);
		}
	}
}
