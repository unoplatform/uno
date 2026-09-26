// [UITest -> runtime-test migration] NOT migrated to a runtime test:
// Device-sensor test (real Gyrometer hardware, Android/iOS only via ActivePlatforms) — Skia's Gyrometer.GetDefault() always returns null (see Gyrometer.unsupported.cs), so there is no sensor to attach/read in a Skia runtime test.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_Devices
{
	[TestFixture]
	public partial class GyrometerTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void When_Gyrometer_Is_Retrived_With_GetDefault()
		{
			Run("UITests.Shared.Windows_Devices.GyrometerTests");

			_app.WaitForText("SensorStatus", "Gyrometer created");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void When_Reading_Is_Attached()
		{
			Run("UITests.Shared.Windows_Devices.GyrometerTests");

			_app.WaitForText("SensorStatus", "Gyrometer created");

			var initialTimestampText = _app.GetText("TimestampRun");
			//initially the timestamp is empty
			Assert.AreEqual("Timestamp:", initialTimestampText.Trim());

			try
			{
				_app.FastTap("AttachReadingButton");

				_app.Wait(TimeSpan.FromMilliseconds(500));

				var firstTimestampSnapshot = _app.GetText("TimestampRun");
				//timestamp is now not empty
				Assert.AreNotEqual("Timestamp:", firstTimestampSnapshot.Trim());
			}
			finally
			{
				_app.FastTap("DetachReadingButton");
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void When_Reading_Is_Attached_And_Waits()
		{
			Run("UITests.Shared.Windows_Devices.GyrometerTests");

			_app.WaitForText("SensorStatus", "Gyrometer created");

			try
			{
				_app.FastTap("AttachReadingButton");

				_app.Wait(TimeSpan.FromMilliseconds(500));
				var firstTimestampSnapshot = _app.GetText("TimestampRun");

				_app.Wait(TimeSpan.FromMilliseconds(1500));
				var secondTimestampSnapshot = _app.GetText("TimestampRun");

				//timestamp should change - readings are received
				Assert.AreNotEqual(firstTimestampSnapshot, secondTimestampSnapshot);
			}
			finally
			{
				_app.FastTap("DetachReadingButton");
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void When_Reading_Is_Attached_And_Detaches()
		{
			Run("UITests.Shared.Windows_Devices.GyrometerTests");

			_app.WaitForText("SensorStatus", "Gyrometer created");

			try
			{
				_app.FastTap("AttachReadingButton");
				_app.Wait(TimeSpan.FromMilliseconds(500));
				_app.FastTap("DetachReadingButton");

				//wait a bit to make sure last invoke finishes
				_app.Wait(TimeSpan.FromMilliseconds(250));
				var firstTimeSnapshot = _app.GetText("TimestampRun");

				_app.Wait(TimeSpan.FromMilliseconds(500));
				var secondTimeSnapshot = _app.GetText("TimestampRun");

				//timestamp should not have changed
				Assert.AreEqual(firstTimeSnapshot, secondTimeSnapshot);
			}
			finally
			{
				_app.FastTap("DetachReadingButton");
			}
		}
	}
}
