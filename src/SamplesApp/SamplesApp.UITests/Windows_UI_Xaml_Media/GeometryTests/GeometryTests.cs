#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.GeometryTests
{
	[TestFixture]
	public partial class GeometryTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Not implemented on other platforms
		public void When_EllipseGeometry()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.PathTestsControl.Path_EllipseGeometry");

			_app.WaitForElement("HostBorder");

			var rect = _app.GetPhysicalRect("HostBorder");

			var scrn = TakeScreenshot("Rendered", ignoreInSnapshotCompare: true);

			ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, Color.Red);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Not implemented on other platforms
		public void When_LineGeometry()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.PathTestsControl.Path_LineGeometry");

			_app.WaitForElement("HostBorder");

			var rect = _app.GetPhysicalRect("HostBorder");

			var scrn = TakeScreenshot("Rendered", ignoreInSnapshotCompare: true);

			ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, Color.Green);
		}
	}
}
