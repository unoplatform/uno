#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Input;

[TestClass]
[RunsOnUIThread]
public class Given_BrowserPointerInputSource
{
	// Values mirror the private HtmlPointerEvent / HtmlPointerButtonsState / HtmlPointerButtonUpdate
	// enums in BrowserPointerInputSource, which themselves match the DOM PointerEvent contract.
	private const int PointerDown = 1 << 2;
	private const int PointerUp = 1 << 3;
	private const int ButtonsX1 = 8; // back button held
	private const int ButtonsX2 = 16; // forward button held
	private const int UpdateX1 = 3; // back button updated
	private const int UpdateX2 = 4; // forward button updated

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Wasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23509")]
	[DataRow(PointerDown, ButtonsX2, UpdateX2, PointerUpdateKind.XButton2Pressed, DisplayName = "Forward button down => XButton2Pressed")]
	[DataRow(PointerUp, 0, UpdateX2, PointerUpdateKind.XButton2Released, DisplayName = "Forward button up => XButton2Released")]
	[DataRow(PointerDown, ButtonsX1, UpdateX1, PointerUpdateKind.XButton1Pressed, DisplayName = "Back button down => XButton1Pressed")]
	[DataRow(PointerUp, 0, UpdateX1, PointerUpdateKind.XButton1Released, DisplayName = "Back button up => XButton1Released")]
	public void When_XButton_Then_Reports_Matching_UpdateKind(int @event, int buttons, int buttonUpdate, PointerUpdateKind expected)
	{
		var actual = RaiseNativePointerEvent((byte)@event, buttons, buttonUpdate);

		Assert.AreEqual(expected, actual);
	}

	private static PointerUpdateKind RaiseNativePointerEvent(byte @event, int buttons, int buttonUpdate)
	{
		var type =
			Type.GetType("Uno.UI.Runtime.BrowserPointerInputSource, Uno.UI")
			?? Type.GetType("Uno.UI.Runtime.Skia.BrowserPointerInputSource, Uno.UI.Runtime.Skia.WebAssembly.Browser");

		Assert.IsNotNull(type, "BrowserPointerInputSource type was not found in the loaded WASM assemblies.");

		// Bypass the constructor: it calls into JS to register the source, which we don't want from a test.
		var source = RuntimeHelpers.GetUninitializedObject(type);

		PointerEventArgs? captured = null;
		TypedEventHandler<object, PointerEventArgs> handler = (_, e) => captured = e;

		var pressed = type.GetEvent("PointerPressed")!;
		var released = type.GetEvent("PointerReleased")!;
		pressed.AddEventHandler(source, handler);
		released.AddEventHandler(source, handler);

		try
		{
			var onNativeEvent = type.GetMethod("OnNativeEvent", BindingFlags.NonPublic | BindingFlags.Static)!;
			onNativeEvent.Invoke(null, new object[]
			{
				source,
				@event,
				/* timestamp */ 0d,
				/* deviceType */ (int)Windows.Devices.Input.PointerDeviceType.Mouse,
				/* pointerId */ 1d,
				/* x */ 0d,
				/* y */ 0d,
				/* ctrl */ false,
				/* shift */ false,
				buttons,
				buttonUpdate,
				/* pressure */ 0.5d,
				/* wheelDeltaX */ 0d,
				/* wheelDeltaY */ 0d,
				/* hasRelatedTarget */ false,
			});
		}
		finally
		{
			pressed.RemoveEventHandler(source, handler);
			released.RemoveEventHandler(source, handler);
		}

		Assert.IsNotNull(captured, "BrowserPointerInputSource did not raise a pointer event.");

		return captured!.CurrentPoint.Properties.PointerUpdateKind;
	}
}
