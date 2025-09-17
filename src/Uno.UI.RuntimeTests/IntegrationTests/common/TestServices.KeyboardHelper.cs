using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation.Metadata;

#if HAS_UNO
using DirectUI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
#endif

namespace Private.Infrastructure
{
	public partial class TestServices
	{
		public static class KeyboardHelper
		{
			static KeyboardHelper()
			{
				// a-z
				for (VirtualKey key = VirtualKey.A; key <= VirtualKey.Z; key++)
				{
					m_vKeyMapping.Add(char.ToLower((char)key).ToString(), key);
				}

				// 0-9
				for (VirtualKey key = VirtualKey.Number0; key <= VirtualKey.Number9; key++)
				{
					var number = key - VirtualKey.Number0;
					m_vKeyMapping.Add(number.ToString(CultureInfo.InvariantCulture), key);
				}

				// f1 - f24
				for (VirtualKey key = VirtualKey.F1; key <= VirtualKey.F24; key++)
				{
					var number = (key - VirtualKey.F1) + 1;
					m_vKeyMapping.Add("f" + number.ToString(CultureInfo.InvariantCulture), key);
				}
			}

			private static Dictionary<string, VirtualKey> m_vKeyMapping = new Dictionary<string, VirtualKey>()
			{
				{"cancel",                      VirtualKey.Cancel},
				{"backspace",                   VirtualKey.Back},
				{"clear",                       VirtualKey.Clear},
				{"enter",                       VirtualKey.Enter},
				{"equal",                       (global::Windows.System.VirtualKey)187},
				{"return",                      VirtualKey.Enter},
				{"numpadenter",                 VirtualKey.Enter},
				{"shift",                       VirtualKey.Shift},
				{"pause",                       VirtualKey.Pause},
				{"capslock",                    VirtualKey.CapitalLock},
				{"kanamode",                    VirtualKey.Kana},
				{"hangulmode",                  VirtualKey.Hangul},
				{"junjamode",                   VirtualKey.Junja},
				{"finalmode",                   VirtualKey.Final},
				{"hanjamode",                   VirtualKey.Hanja},
				{"kanjimode",                   VirtualKey.Kanji},
				{"imeconvert",                  VirtualKey.Convert},
				{"imenonconvert",               VirtualKey.NonConvert},
				{"imeaccept",                   VirtualKey.Accept},
				{"imemodechange",               VirtualKey.ModeChange},
				{"prior",                       VirtualKey.Back},
				{"pageup",                      VirtualKey.PageUp},
				{"pagedown",                    VirtualKey.PageDown},
				{"end",                         VirtualKey.End},
				{"home",                        VirtualKey.Home},
				{"select",                      VirtualKey.Select},
				{"print",                       VirtualKey.Print},
				{"execute",                     VirtualKey.Execute},
				{"printscreen",                 VirtualKey.Snapshot},
				{"snapshot",                    VirtualKey.Snapshot},
				{"insert",                      VirtualKey.Insert},
				{"delete",                      VirtualKey.Delete},
				{"del",                         VirtualKey.Delete},
				{"help",                        VirtualKey.Help},
				{"lwin",                        VirtualKey.LeftWindows},
				{"rwin",                        VirtualKey.RightWindows},
				{"apps",                        VirtualKey.Application},
				{"sleep",                       VirtualKey.Sleep},
				{"*",                           VirtualKey.Multiply},
				{"+",                           VirtualKey.Add},
				{"separator",                   VirtualKey.Separator},
				{"-",                           VirtualKey.Subtract},
				{"/",                           VirtualKey.Divide},
				{"numlock",                     VirtualKey.NumberKeyLock},
				{"scrolllock",                  VirtualKey.Scroll},
				{"lshift",                      VirtualKey.LeftShift},
				{"rshift",                      VirtualKey.RightShift},
				{"lctrl",                       VirtualKey.LeftControl},
				{"rctrl",                       VirtualKey.RightControl},
				{"alt",                         VirtualKey.Menu},
				{"lalt",                        VirtualKey.LeftMenu},
				{"ralt",                        VirtualKey.RightMenu},
				{"space",                       VirtualKey.Space},
				{" ",                           VirtualKey.Space},
				{"down",                        VirtualKey.Down},
				{"up",                          VirtualKey.Up},
				{"left",                        VirtualKey.Left},
				{"right",                       VirtualKey.Right},
				{"tab",                         VirtualKey.Tab},
				{"esc",                         VirtualKey.Escape},
				{"ctrl",                        VirtualKey.Control},
				{"ctrlscan",                    VirtualKey.Control},
				{"GamepadA",                    VirtualKey.GamepadA},
				{"GamepadB",                    VirtualKey.GamepadB},
				{"GamepadDpadRight",            VirtualKey.GamepadDPadRight},
				{"GamepadDpadDown",             VirtualKey.GamepadDPadDown},
				{"GamepadDpadUp",               VirtualKey.GamepadDPadUp},
				{"GamepadDpadLeft",             VirtualKey.GamepadDPadLeft},
				{"GamepadLeftShoulder",         VirtualKey.GamepadLeftShoulder},
				{"GamepadLeftTrigger",          VirtualKey.GamepadLeftTrigger},
				{"GamepadRightShoulder",        VirtualKey.GamepadRightShoulder},
				{"GamepadRightTrigger",         VirtualKey.GamepadRightTrigger},
				{"GamePadLeftThumbStickRight",  VirtualKey.GamepadLeftThumbstickRight},
				{"GamePadLeftThumbStickDown",   VirtualKey.GamepadLeftThumbstickDown},
				{"GamePadLeftThumbStickUp",     VirtualKey.GamepadLeftThumbstickUp},
				{"GamePadLeftThumbStickLeft",   VirtualKey.GamepadLeftThumbstickLeft},
				{"GamePadRightThumbStickRight", VirtualKey.GamepadRightThumbstickRight},
				{"GamePadRightThumbStickDown",  VirtualKey.GamepadRightThumbstickDown},
				{"GamePadRightThumbStickUp",    VirtualKey.GamepadRightThumbstickUp},
				{"GamePadRightThumbStickLeft",  VirtualKey.GamepadRightThumbstickLeft},
				{"GamePadMenu",                 VirtualKey.GamepadMenu},
			};

			private static bool TargetSupportsPreviewKeyEvents() => ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.UIElement, Uno.UI", "PreviewKeyDownEvent");

			public static async Task PressKeySequence(string keys, UIElement element = null)
			{
#if !WINAPPSDK
				if (string.IsNullOrEmpty(keys))
				{
					return;
				}

				element ??= FocusManager.GetFocusedElement(WindowHelper.XamlRoot) as UIElement;
				if (element is null)
				{
					return;
				}

				VirtualKeyModifiers activeModifiers = VirtualKeyModifiers.None;

				// In RS3, alt+shift is a hotkey and XAML will not be notified of the
				// 'shift' key. We have the same behavior for the opposite sequence: if
				// the sequence is shift+alt, XAML will not be notified of the 'alt' key.
				// To workaround this behavior, we will ignore failures when sending alt
				// and shift inputs if both are present in the sequence.
				bool areAltAndShiftUsed = keys.Contains("alt") || keys.Contains("shift");

				// This is a cute key sequence mini-language adopted from the previous test framework.
				// It walks through the string and if it's a normal string sends it, or if it's a string
				// specifying special keys, will press those keys. Here are some valid strings:
				// "$d$_ctrl#$d$_-#$u$_-#$u$_ctrl" - Down Ctrl, Down -, Up -, Up Ctrl
				// "Hello" - Down Shift, Down H, Up H, Up Shift, Down e, Up e... etc
				int posStart = 0;
				keys += "#";
				int posEnd = keys.IndexOf("#");
				while (posEnd != -1)
				{
					var keyInstruction = keys.Substring(posStart, posEnd - posStart);

					int keyDownCodePos = keyInstruction.IndexOf("$d$_");
					int keyUpCodePos = keyInstruction.IndexOf("$u$_");

					if (keyDownCodePos == 0 || keyUpCodePos == 0)
					{
						var key = keyInstruction.Substring(4, keyInstruction.Length - 4);
						if (m_vKeyMapping.TryGetValue(key, out var vKey))
						{
							var keyArgs = new KeyRoutedEventArgs(element, vKey, activeModifiers);

							var mainEvent = keyDownCodePos == 0 ? UIElement.KeyDownEvent : UIElement.KeyUpEvent;
							if (TargetSupportsPreviewKeyEvents())
							{
								var previewEvent = keyDownCodePos == 0 ? UIElement.PreviewKeyDownEvent : UIElement.PreviewKeyUpEvent;
								await RaiseOnElementDispatcherAsync(element, previewEvent, keyArgs, true);
							}
							if (!keyArgs.Handled)
							{
								await RaiseOnElementDispatcherAsync(element, mainEvent, keyArgs);
							}
						}

						// If modifiers were changed, update modifiers variable
						if (keyDownCodePos == 0)
						{
							if (key == "shift")
							{
								activeModifiers |= VirtualKeyModifiers.Shift;
							}
							else if (key == "ctrl")
							{
								activeModifiers |= VirtualKeyModifiers.Control;
							}
							else if (key == "alt")
							{
								activeModifiers |= VirtualKeyModifiers.Menu;
							}
						}
						else if (keyUpCodePos == 0)
						{
							if (key == "shift")
							{
								activeModifiers &= ~VirtualKeyModifiers.Shift;
							}
							else if (key == "ctrl")
							{
								activeModifiers &= ~VirtualKeyModifiers.Control;
							}
							else if (key == "alt")
							{
								activeModifiers &= ~VirtualKeyModifiers.Menu;
							}
						}
					}
					else
					{
						// This is a normal string, just send a up/down of each key.
						for (int i = 0; i < keyInstruction.Length; i++)
						{
							var key = keyInstruction.Substring(i, 1);
							bool shouldShift = char.IsUpper(key[0]);
							key = char.ToLower(key[0], CultureInfo.InvariantCulture).ToString();

							if (shouldShift)
							{
								if (m_vKeyMapping.TryGetValue("shift", out var vShiftKey))
								{
									var keyDownArgs = new KeyRoutedEventArgs(element, vShiftKey, VirtualKeyModifiers.None);
									if (TargetSupportsPreviewKeyEvents())
									{
										await RaiseOnElementDispatcherAsync(element, UIElement.PreviewKeyDownEvent, keyDownArgs, true);
									}
									if (!keyDownArgs.Handled)
									{
										await RaiseOnElementDispatcherAsync(element, UIElement.KeyDownEvent, keyDownArgs);
									}
								}
							}

							if (m_vKeyMapping.TryGetValue(key, out var vKey))
							{
								var modifiers = shouldShift ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None;
								var keyDownArgs = new KeyRoutedEventArgs(element, vKey, modifiers);
								if (TargetSupportsPreviewKeyEvents())
								{
									await RaiseOnElementDispatcherAsync(element, UIElement.PreviewKeyDownEvent, keyDownArgs, true);
								}
								if (!keyDownArgs.Handled)
								{
									await RaiseOnElementDispatcherAsync(element, UIElement.KeyDownEvent, keyDownArgs);
								}
								var keyUpArgs = new KeyRoutedEventArgs(element, vKey, modifiers);
								if (TargetSupportsPreviewKeyEvents())
								{
									await RaiseOnElementDispatcherAsync(element, UIElement.PreviewKeyUpEvent, keyUpArgs, true);
								}
								if (!keyUpArgs.Handled)
								{
									await RaiseOnElementDispatcherAsync(element, UIElement.KeyUpEvent, keyUpArgs);
								}
							}

							if (shouldShift)
							{
								if (m_vKeyMapping.TryGetValue("shift", out var vShiftKey))
								{
									var keyUpArgs = new KeyRoutedEventArgs(element, vShiftKey, VirtualKeyModifiers.None);
									if (TargetSupportsPreviewKeyEvents())
									{
										await RaiseOnElementDispatcherAsync(element, UIElement.PreviewKeyUpEvent, keyUpArgs, true);
									}
									if (!keyUpArgs.Handled)
									{
										await RaiseOnElementDispatcherAsync(element, UIElement.KeyUpEvent, keyUpArgs);
									}
								}
							}
						}
					}

					posStart = posEnd + 1;
					posEnd = keys.IndexOf("#", posStart);

#if HAS_UNO
					// To allow TextBox to fully process the key events
					await TestServices.WindowHelper.WaitForIdle();
#endif
				}

				async Task RaiseOnElementDispatcherAsync(UIElement element, RoutedEvent routedEvent, KeyRoutedEventArgs args, bool isTunneling = false)
				{
					bool raiseSynchronously = element.Dispatcher.HasThreadAccess;

					static void UpdateLastInputDeviceType(UIElement element, VirtualKey originalKey)
					{
						// Workaround for not simulating the last input device type correctly yet.
						var inputManager = VisualTree.GetContentRootForElement(element).InputManager;
						if (XboxUtility.IsGamepadNavigationInput(originalKey))
						{
							inputManager.LastInputDeviceType = InputDeviceType.GamepadOrRemote;
						}
						else
						{
							inputManager.LastInputDeviceType = InputDeviceType.Keyboard;
						}
					}

					static void RaiseTunnelingEvent(UIElement element, RoutedEvent routedEvent, KeyRoutedEventArgs args)
					{
						UpdateLastInputDeviceType(element, args.OriginalKey);
						element.SafeRaiseTunnelingEvent(routedEvent, args);
					}

					static void RaiseBubblingEvent(UIElement element, RoutedEvent routedEvent, KeyRoutedEventArgs args)
					{
						UpdateLastInputDeviceType(element, args.OriginalKey);
						element.SafeRaiseEvent(routedEvent, args);
					}

					if (isTunneling)
					{
						if (raiseSynchronously)
						{
							RaiseTunnelingEvent(element, routedEvent, args);
						}
						else
						{
							await element.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RaiseTunnelingEvent(element, routedEvent, args));
						}
					}
					else
					{
						if (raiseSynchronously)
						{
							RaiseBubblingEvent(element, routedEvent, args);
						}
						else
						{
							await element.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RaiseBubblingEvent(element, routedEvent, args));
						}
					}
				}
#endif
			}

			public static async Task Down(UIElement element = null)
			{
				await PressKeySequence("$d$_down#$u$_down", element);
			}

			public static async Task Up(UIElement element = null)
			{
				await PressKeySequence("$d$_up#$u$_up", element);
			}

			public static async Task Left(UIElement element = null)
			{
				await PressKeySequence("$d$_left#$u$_left", element);
			}

			public static async Task Right(UIElement element = null)
			{
				await PressKeySequence("$d$_right#$u$_right", element);
			}

			internal static async Task ShiftTab(UIElement element = null)
			{
				await PressKeySequence("$d$_shift#$d$_tab#$u$_tab#$u$_shift", element);
			}

			public static async Task Tab(UIElement element = null)
			{
				await PressKeySequence("$d$_tab#$u$_tab", element);
			}

			public static async Task PageDown(UIElement element = null)
			{
				await PressKeySequence("$d$_pagedown#$u$_pagedown", element);
			}

			public static async Task Escape(UIElement element = null)
			{
				await PressKeySequence("$d$_esc#$u$_esc", element);
			}

			public static async Task Enter(UIElement element = null)
			{
				await PressKeySequence("$d$_enter#$u$_enter", element);
			}

			public static async Task Space(UIElement element = null)
			{
				await PressKeySequence("$d$_space#$u$_space", element);

			}
			public static async Task Backspace(UIElement element = null)
			{
				await PressKeySequence("$d$_backspace#$u$_backspace", element);
			}

			public static async Task Delete(UIElement element = null)
			{
				await PressKeySequence("$d$_delete#$u$_delete", element);
			}

			public static async Task CtrlTab(UIElement element = null)
			{
				await PressKeySequence("$d$_ctrlscan#$d$_tab#$u$_tab#$u$_ctrlscan", element);
			}

			public static async Task GamepadA(UIElement element = null)
			{
				await PressKeySequence("$d$_GamepadA#$u$_GamepadA", element);
			}

			public static async Task GamepadB(UIElement element = null)
			{
				await PressKeySequence("$d$_GamepadB#$u$_GamepadB", element);
			}

			public static async Task GamepadDpadRight(UIElement element = null)
			{
				await PressKeySequence("$d$_GamepadDpadRight#$u$_GamepadDpadRight", element);
			}

			public static async Task GamepadDpadLeft(UIElement element = null)
			{
				await PressKeySequence("$d$_GamepadDpadLeft#$u$_GamepadDpadLeft", element);
			}
			public static async Task GamepadDpadUp(UIElement element = null)
			{
				await PressKeySequence("$d$_GamepadDpadUp#$u$_GamepadDpadUp", element);
			}
			public static async Task GamepadDpadDown(UIElement element = null)
			{
				await PressKeySequence("$d$_GamepadDpadDown#$u$_GamepadDpadDown", element);
			}

			/// <param name="text">Assuming lowercase text. To add capitalization, use <see cref="PressKeySequence"/></param>
			public static async Task InputText(string text, UIElement element = null)
			{
				var sequence = text
					.ToLower()
					.Select(c => $"$d$_{c}#$u$_{c}#")
					.Aggregate("", (a, b) => a + b)
					[..^1]; // drop last #

				await PressKeySequence(sequence, element);
			}
		}
	}
}
