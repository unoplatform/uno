using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.Animation_Tests
{
	[TestFixture]
	public partial class SequentialAnimations_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void SequentialAnimations()
		{
			// Navigate to this x:Class control name
			Run("SamplesApp.Windows_UI_Xaml_Media.Animation.SequentialAnimationsPage");
		}
	}
}
