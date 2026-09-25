#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using PointerEventArgs = global::Windows.UI.Core.PointerEventArgs;
using System.Reflection;
using System.Runtime.CompilerServices;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Input;

[TestClass]
[RunsOnUIThread]
public class Given_BrowserPointerInputSource
{
	// Values mirror the private HtmlPointerEvent / HtmlPointerButtonsState / HtmlPointerButtonUpdate
	// enums in BrowserPointerInputSource, which themselves match the DOM PointerEvent contract.
	private const int PointerDown = 1 << 2;
	private const int PointerUp = 1 << 3;
	private const int Wheel = 1 << 7;
	private const int ButtonsX1 = 8; // back button held
	private const int ButtonsX2 = 16; // forward button held
	private const int UpdateX1 = 3; // back button updated
	private const int UpdateX2 = 4; // forward button updated

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
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

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public void When_WheelDeltaIsFractional_Then_AccumulatesAcrossEvents()
	{
		// Four consecutive trackpad ticks of 0.4 CSS px each (deltaMode=PIXEL). Each event reports the
		// raw fractional delta as-is (unlike Win32/X11, where the OS already accumulates fractional
		// notches into an int before delivering the message), so the source must carry the remainder
		// across events instead of truncating every event to 0.
		var deltas = RaiseWheelEvents(wheelDeltaY: -0.4, count: 4);

		// 0.4+0.4+0.4+0.4 = 1.6px total: the third tick is the first to cross a whole unit (1), and the
		// leftover 0.6 remainder isn't enough to cross another one, so only one event carries a delta.
		CollectionAssert.AreEqual(new[] { 1 }, deltas);
	}

	private static List<int> RaiseWheelEvents(double wheelDeltaY, int count)
	{
		var type = ResolveBrowserPointerInputSourceType();
		var source = CreateUninitializedSource(type);

		var deltas = new List<int>();
		TypedEventHandler<object, PointerEventArgs> handler = (_, e) => deltas.Add(e.CurrentPoint.Properties.MouseWheelDelta);

		var wheelChanged = type.GetEvent("PointerWheelChanged")!;
		wheelChanged.AddEventHandler(source, handler);

		try
		{
			var onNativeEvent = type.GetMethod("OnNativeEvent", BindingFlags.NonPublic | BindingFlags.Static)!;
			for (var i = 0; i < count; i++)
			{
				onNativeEvent.Invoke(null, new object[]
				{
					source,
					(byte)Wheel,
					/* timestamp */ 0d,
					/* deviceType */ (int)Windows.Devices.Input.PointerDeviceType.Mouse,
					/* pointerId */ 1d,
					/* x */ 0d,
					/* y */ 0d,
					/* ctrl */ false,
					/* shift */ false,
					/* buttons */ 0,
					/* buttonUpdate */ 0,
					/* pressure */ 0.5d,
					/* wheelDeltaX */ 0d,
					wheelDeltaY,
					/* hasRelatedTarget */ false,
				});
			}
		}
		finally
		{
			wheelChanged.RemoveEventHandler(source, handler);
		}

		return deltas;
	}

	private static Type ResolveBrowserPointerInputSourceType()
	{
		var type =
			Type.GetType("Uno.UI.Runtime.BrowserPointerInputSource, Uno.UI")
			?? Type.GetType("Uno.UI.Runtime.Skia.BrowserPointerInputSource, Uno.UI.Runtime.Skia.WebAssembly.Browser");

		Assert.IsNotNull(type, "BrowserPointerInputSource type was not found in the loaded WASM assemblies.");

		return type!;
	}

	// Bypass the constructor: it calls into JS to register the source, which we don't want from a test.
	private static object CreateUninitializedSource(Type type) => RuntimeHelpers.GetUninitializedObject(type);

	private static PointerUpdateKind RaiseNativePointerEvent(byte @event, int buttons, int buttonUpdate)
	{
		var type = ResolveBrowserPointerInputSourceType();
		var source = CreateUninitializedSource(type);

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

		return (PointerUpdateKind)captured!.CurrentPoint.Properties.PointerUpdateKind;
	}
}
