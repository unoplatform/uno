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
		internal static async Task Cancel(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					await KeyboardHelper.Escape(element);
					break;

				case InputDevice.Gamepad:
					await KeyboardHelper.GamepadB(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static async Task Accept(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					await KeyboardHelper.Space(element);
					break;

				case InputDevice.Gamepad:
					await KeyboardHelper.GamepadA(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static async Task Right(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					await KeyboardHelper.Right(element);
					break;

				case InputDevice.Gamepad:
					await KeyboardHelper.GamepadDpadRight(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static async Task Left(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					await KeyboardHelper.Left(element);
					break;

				case InputDevice.Gamepad:
					await KeyboardHelper.GamepadDpadLeft(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static async Task Up(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					await KeyboardHelper.Up(element);
					break;

				case InputDevice.Gamepad:
					await KeyboardHelper.GamepadDpadUp(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}

		internal static async Task Down(InputDevice device, UIElement element = null)
		{
			switch (device)
			{
				case InputDevice.Keyboard:
					await KeyboardHelper.Down(element);
					break;

				case InputDevice.Gamepad:
					await KeyboardHelper.GamepadDpadDown(element);
					break;
				default:
					throw new Exception("Invalid input device.");
			}
		}
	}
}
