using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.FrameTests
{
	[TestClass]
	public class Given_Frame
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_RemovedPage()
		{
			var SUT = new Frame()
			{
			};

			SUT.Navigate(typeof(MyPage));

			var myPage1 = SUT.Content as MyPage;
			Assert.IsNotNull(myPage1);
			Assert.AreEqual(SUT, myPage1.Frame);

			SUT.Navigate(typeof(MyPage));

			var myPage2 = SUT.Content as MyPage;
			Assert.IsNotNull(myPage2);
			Assert.AreEqual(SUT, myPage2.Frame);

			SUT.GoBack();

			Assert.AreEqual(myPage1, SUT.Content);
			Assert.IsNotNull(myPage2.Frame);

			SUT.Navigate(typeof(MyPage));

			var myPage3 = SUT.Content as MyPage;

			Assert.AreEqual(myPage3, SUT.Content);
			Assert.IsNull(myPage2.Frame);
		}
	}

	class MyPage : Page
	{
	}
}
