using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;

#if WINAPPSDK
using Uno.UI.Toolkit.Extensions;
#endif

#if HAS_UNO_WINUI
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input
{
	[TestClass]
	public class Given_GestureRecognizer
	{
		[TestMethod]
		[RunsOnUIThread]
		// Note: Timeout is handled inside the test method by the CTS. No need to add an attribute here.
#if !HAS_INPUT_INJECTOR || (!HAS_UNO_WINUI && !WINAPPSDK) // Requires pointer injection and WinUI API
		[Ignore("This test is not supported on this platform.")]
#endif
#if !WINAPPSDK
		[DataRow(PointerDeviceType.Mouse)]
#endif
		[DataRow(PointerDeviceType.Touch)]
		public async Task When_ManipulateWithVelocity_Then_InertiaKicksIn(PointerDeviceType type)
		{
			var sample = new Manipulation_Inertia();
			await UITestHelper.Load(sample);

			var started = false;
			var completed = new TaskCompletionSource();
			var completedTimeout = new CancellationTokenSource(30000);
			using var _ = completedTimeout.Token.Register(() => completed.TrySetException(new TimeoutException("Cannot get complete in given delay.")));
			sample.IsRunningChanged += (snd, isRunning) =>
			{
				if (isRunning)
				{
					started = true;
				}
				else
				{
					completed.TrySetResult();
				}
			};

			var origin = sample.Element.GetAbsoluteBounds().GetCenter();
			var pointer = (InputInjector.TryCreate() ?? throw new InvalidOperationException("Input injection is not supported on this device")).GetPointer(type);

			pointer.Press(origin);

			pointer.MoveTo(new Point(origin.X, origin.Y + 25), steps: 10); // 1 step per ms!

			started.Should().Be(true, "Manipulation should have started.");

			pointer.Release();

			await completed.Task;

			sample.Validate().Should().BeTrue();
		}
	}
}
