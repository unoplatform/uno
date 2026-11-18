using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Input;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.Tests.Windows_UI_Input;

[UITestClass]
public class Given_InjectedInputState
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26163")]
	public void When_Touch_Sequence_Injected_Then_FrameId_Advances_And_Is_Not_Zero()
	{
		var frameIds = new List<uint>();

		// Two press/move/release sequences on the same injector state, like a test that taps twice.
		var state = new InjectedInputState(PointerDeviceType.Touch);
		for (var sequence = 0; sequence < 2; sequence++)
		{
			if (sequence > 0)
			{
				state.StartNewSequence();
			}

			foreach (var options in new[]
			{
				InjectedInputPointerOptions.New | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.PointerDown,
				InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.InContact,
				InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.InContact,
				InjectedInputPointerOptions.PointerUp,
			})
			{
				var args = new InjectedInputTouchInfo
				{
					PointerInfo = new InjectedInputPointerInfo { PointerId = 1, PointerOptions = options }
				}.ToEventArgs(state);
				state.Update(args);
				frameIds.Add(args.CurrentPoint.FrameId);
			}
		}

		Assert.AreNotEqual(0u, frameIds[0]);
		for (var i = 1; i < frameIds.Count; i++)
		{
			Assert.IsTrue(frameIds[i] > frameIds[i - 1], $"FrameId of frame {i} ({frameIds[i]}) is not greater than the previous one ({frameIds[i - 1]})");
		}
	}
}
