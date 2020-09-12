using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Toolkit
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void ElevatedView_Dimensions_Validation()
		{
			Run("UITests.Toolkit.ElevatedView_Dimensions");

			_app.WaitForElement("elevatedViewText");

			var elevatedView = _app.Marked("elevatedView");

			using (new AssertionScope())
			{
				elevatedView.FirstResult().Rect.X.Should().Be(42f);
				elevatedView.FirstResult().Rect.Y.Should().Be(98f);
				elevatedView.FirstResult().Rect.Width.Should().Be(200f);
				elevatedView.FirstResult().Rect.Height.Should().Be(160f);
			}
		}
	}
}
