// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Text;

using Private.Infrastructure;
using Microsoft.UI.Xaml.Tests.Common;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Tests.Common
{
	public sealed class KeyboardAcceleratorEventOrderingTester : IDisposable
	{
		public StringBuilder EventOrder;
		private readonly FrameworkElement[] ElementsToTest;

		public KeyboardAcceleratorEventOrderingTester(FrameworkElement[] elements, StringBuilder eventOrder)
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

			UIExecutor.Execute(() =>
			{
				for (int i = 0; i < ElementsToTest.Length; i++)
				{
					this.AddEventsToFrameworkElement(ElementsToTest[i]);
				}
			});
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

		public void Dispose()
		{
			UIExecutor.Execute(() =>
			{
				for (int i = 0; i < ElementsToTest.Length; i++)
				{
					this.RemoveEventsFromFrameworkElement(ElementsToTest[i]);
				}
			});
		}
	}
}
