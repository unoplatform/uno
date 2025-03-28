using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Helpers;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.UI.Extensions;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_NonFrameworkElement_TopLevel
	{
		[TestMethod]
		public void When_AttachingToEvent()
		{
			var SUT = new When_NonFrameworkElement_Event();

			SUT.RaiseMyEvent();

			Assert.AreEqual(1, SUT.MyEventCalled);
		}
	}
}
