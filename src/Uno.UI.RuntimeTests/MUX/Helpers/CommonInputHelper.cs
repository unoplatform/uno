using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal enum InputDevice
	{
		Keyboard,
		Gamepad,
	}

	internal class CommonInputHelper
	{
		internal static void Cancel(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					KeyboardHelper.Escape(element);
					break;

				case InputDevice.Gamepad:
					KeyboardHelper.GamepadB(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static void Accept(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					KeyboardHelper.Space(element);
					break;

				case InputDevice.Gamepad:
					KeyboardHelper.GamepadA(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static void Right(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					KeyboardHelper.Right(element);
					break;

				case InputDevice.Gamepad:
					KeyboardHelper.GamepadDpadRight(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static void Left(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					KeyboardHelper.Left(element);
					break;

				case InputDevice.Gamepad:
					KeyboardHelper.GamepadDpadLeft(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static void Up(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					KeyboardHelper.Up(element);
					break;

				case InputDevice.Gamepad:
					KeyboardHelper.GamepadDpadUp(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static void Down(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					KeyboardHelper.Down(element);
					break;

				case InputDevice.Gamepad:
					KeyboardHelper.GamepadDpadDown(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}
	}
}
