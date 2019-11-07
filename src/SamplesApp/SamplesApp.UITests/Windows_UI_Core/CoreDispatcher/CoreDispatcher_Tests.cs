using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Core.CoreDispatcher
{
  [TestFixture]
  public partial class CoreDispatcher_Tests : SampleControlUITestBase
  {
	[Test]
	[AutoRetry]
	public void RunAsyncExtensions()
	{
	  Run("UITests.Shared.Windows_UI_Core.CoreDispatcherControl.CoreDispatcherControl");

	  _app.Marked("RunAsyncButton").Tap();


	  Thread.Sleep(1200);

	  _app.Marked("Result").WithText("Success");
	}
  }
}
