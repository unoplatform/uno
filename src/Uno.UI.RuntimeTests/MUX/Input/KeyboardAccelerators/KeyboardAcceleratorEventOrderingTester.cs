// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Private.Infrastructure;
using Windows.Foundation;

namespace Windows.UI.Xaml.Tests.Common
{
	public sealed class KeyboardAcceleratorEventOrderingTester : IAsyncDisposable
	{
		public StringBuilder EventOrder;
		private readonly FrameworkElement[] ElementsToTest;

		private KeyboardAcceleratorEventOrderingTester(FrameworkElement[] elements, StringBuilder eventOrder)
		{
			this.EventOrder = eventOrder;
			this.ElementsToTest = elements;

			FrameworkElementKeyDownHandler = new KeyEventHandler((source, e) =>
			{
				FrameworkElement sourceAsFrameworkElement = (FrameworkElement)source;
				var msg = "[" + sourceAsFrameworkElement.Name + "KeyDown:" + e.OriginalKey.ToString() + GetHandled(e.Handled) + "]";
				EventOrder.Append(msg);
				//Log.Comment($"FrameworkElementKeyDownHandler:{msg}");
			});

			FrameworkElementProcessKeyboardAcceleratorsHandler = new TypedEventHandler<UIElement, ProcessKeyboardAcceleratorEventArgs>((source, e) =>
			{
				FrameworkElement sourceAsFrameworkElement = (FrameworkElement)source;
				var msg = "[" + sourceAsFrameworkElement.Name + "ProcessKeyboardAccelerators:" + e.Key.ToString() + ":" + e.Modifiers.ToString() + GetHandled(e.Handled) + "]";
				EventOrder.Append(msg);
				//Log.Comment($"FrameworkElementProcessKeyboardAcceleratorsHandler:{msg}");
			});

			//TestServices.RunOnUIThread(() =>
			{
				for (int i = 0; i < ElementsToTest.Length; i++)
				{
					this.AddEventsToFrameworkElement(ElementsToTest[i]);
				}
			}
		}

		public static async Task<KeyboardAcceleratorEventOrderingTester> CreateAsync(FrameworkElement[] elements, StringBuilder eventOrder)
		{
			KeyboardAcceleratorEventOrderingTester tester = null;
			await TestServices.RunOnUIThread(() => tester = new KeyboardAcceleratorEventOrderingTester(elements, eventOrder));
			return tester;
		}

		private void AddEventsToFrameworkElement(FrameworkElement element)
		{
			element.AddHandler(
				UIElement.KeyDownEvent,
				FrameworkElementKeyDownHandler,
				true /*handledEventsToo*/
				);
			element.ProcessKeyboardAccelerators += FrameworkElementProcessKeyboardAcceleratorsHandler;
		}

		private void RemoveEventsFromFrameworkElement(FrameworkElement element)
		{
			element.RemoveHandler(
				UIElement.KeyDownEvent,
				FrameworkElementKeyDownHandler);
			element.ProcessKeyboardAccelerators -= FrameworkElementProcessKeyboardAcceleratorsHandler;
		}

		private string GetHandled(bool handled)
		{
			if (handled)
			{
				return ":Handled";
			}
			return "";
		}

		KeyEventHandler FrameworkElementKeyDownHandler;
		TypedEventHandler<UIElement, ProcessKeyboardAcceleratorEventArgs> FrameworkElementProcessKeyboardAcceleratorsHandler;

		public async ValueTask DisposeAsync()
		{
			await TestServices.RunOnUIThread(() =>
			{
				for (int i = 0; i < ElementsToTest.Length; i++)
				{
					this.RemoveEventsFromFrameworkElement(ElementsToTest[i]);
				}
			});
		}
	}
}
