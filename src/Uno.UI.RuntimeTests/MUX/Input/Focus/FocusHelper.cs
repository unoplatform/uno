// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusHelper.cs

#if !WINAPPSDK
using System;
using System.Threading.Tasks;
using Common;
using Microsoft/* UWP don't rename */.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using UIExecutor = MUXControlsTestApp.Utilities.RunOnUIThread;

namespace Uno.UI.RuntimeTests.MUX.Input.Focus
{
	public static class FocusHelper
	{
		public static async Task EnsureFocusAsync(Hyperlink element, FocusState focusState, UInt32 Attempts = 1)
		{
			bool gotFocus = false;

			while (Attempts > 0 && !gotFocus)
			{
				Attempts--;

				//TestServicesExtensions.EnsureForegroundWindow();

				using (var eventTester = new EventTester<Hyperlink, RoutedEventArgs>(element, "GotFocus"))
				using (var eventTesterFocusMgr = EventTester<object, FocusManagerGotFocusEventArgs>.FromStaticEvent<FocusManager>("GotFocus"))
				{
					UIExecutor.Execute(() =>
					{
						if (element.FocusState == focusState && FocusManager.GetFocusedElement(element.XamlRoot).Equals(element))
						{
							// The element is already focused with the expected focus state
							Log.Comment("Focus was already set on this element");
							gotFocus = true;
						}
						else
						{
							Log.Comment("Setting focus to the element...");
							element.Focus(focusState);
						}
					});

					await Task.Delay(100);
					//eventTester.WaitForNoThrow(TimeSpan.FromMilliseconds(4000));
					//eventTesterFocusMgr.WaitForNoThrow(TimeSpan.FromMilliseconds(4000));
					gotFocus = gotFocus == false ? eventTester.HasFired : true;
				}

				await TestServices.WindowHelper.WaitForIdle();
			}

			//if (!gotFocus)
			//{
			//	TestServices.Utilities.CaptureScreen("FocusTestHelper");
			//}
			Verify.IsTrue(gotFocus);
		}

		public static async Task EnsureFocusAsync(UIElement element, FocusState focusState, UInt32 Attempts = 3)
		{
			bool gotFocus = false;

			while (Attempts > 0 && !gotFocus)
			{
				Attempts--;

				//TestServicesExtensions.EnsureForegroundWindow();

				using (var eventTester = new EventTester<UIElement, RoutedEventArgs>(element, "GotFocus"))
				using (var eventTesterFocusMgr = EventTester<object, FocusManagerGotFocusEventArgs>.FromStaticEvent<FocusManager>("GotFocus"))
				{
					UIExecutor.Execute(() =>
					{
						if (element.FocusState == focusState && FocusManager.GetFocusedElement(element.XamlRoot).Equals(element))
						{
							// The element is already focused with the expected focus state
							Log.Comment("Focus was already set on this element");
							gotFocus = true;
						}
						else
						{
							Log.Comment("Setting focus to the element...");
							element.Focus(focusState);
						}
					});

					await Task.Delay(100);
					//eventTester.WaitForNoThrow(TimeSpan.FromMilliseconds(4000));
					//eventTesterFocusMgr.WaitForNoThrow(TimeSpan.FromMilliseconds(4000));
					gotFocus = gotFocus == false ? eventTester.HasFired : true;
				}

				await TestServices.WindowHelper.WaitForIdle();
			}

			//if (!gotFocus)
			//{
			//	TestServices.Utilities.CaptureScreen("FocusTestHelper");
			//}
			Verify.IsTrue(gotFocus);
		}
	}
}
#endif
