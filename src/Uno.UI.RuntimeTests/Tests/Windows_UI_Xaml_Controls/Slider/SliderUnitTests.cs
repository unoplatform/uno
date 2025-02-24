// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference SliderUnitTests.cpp

#if HAS_UNO
using Windows.UI.Xaml.Controls;
using Uno.UI.RuntimeTests;
using static Private.Infrastructure.TestServices;
using Windows.System;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Tests.Controls;

[TestClass]
[RunsOnUIThread]
public class SliderUnitTests
{
	[TestMethod]
	public void ValidateKeyDownWithNavigationOrGamepad()
	{
		Slider sObject = new Slider();

		bool handled;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadDPadRight, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 1);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadDPadLeft, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 0);

		sObject.Value = 0;

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadLeftThumbstickRight, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 1);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadLeftThumbstickLeft, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 0);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadRightShoulder, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, sObject.Maximum);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadLeftShoulder, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, sObject.Minimum);
	}

	[TestMethod]
	public void ValidateKeyDown()
	{
		Slider sObject = new Slider();

		bool handled;

		Slider.KeyProcess.KeyDown(VirtualKey.Up, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 1);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.Left, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 0);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.End, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, sObject.Maximum);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.Home, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, sObject.Minimum);
	}

	[TestMethod]
	public void ValidateKeyDownReversedWithNavigationOrGamepad()
	{
		Slider sObject = new Slider();

		sObject.IsDirectionReversed = true;
		bool handled;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadDPadLeft, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 1);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadDPadRight, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 0);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadLeftThumbstickLeft, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 1);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.GamepadLeftThumbstickRight, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 0);
	}

	void ValidateKeyDownReversed()
	{
		Slider sObject = new Slider();

		sObject.IsDirectionReversed = true;
		bool handled;
		Slider.KeyProcess.KeyDown(VirtualKey.Left, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 1);

		handled = false;
		Slider.KeyProcess.KeyDown(VirtualKey.Up, sObject, out handled);
		VERIFY_ARE_EQUAL(handled, true);
		VERIFY_ARE_EQUAL(sObject.Value, 0);
	}
}
#endif
