using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Animation
{
	[TestFixture]
	public partial class TranslateTransformClickability : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Test_Clickability()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Media.Transform.Border_With_TranslateTransform_Clickable");

			Assert.AreEqual("Test1", _app.GetText("Test1"));
			_app.FastTap("Test1");
			Assert.AreEqual("Changed", _app.GetText("Test1"));
		}
	}
}
