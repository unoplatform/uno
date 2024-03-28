// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusEventOrderingTester.cs

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using UIExecutor = MUXControlsTestApp.Utilities.RunOnUIThread;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Tests.Common
{
	public sealed class FocusEventOrderingTester : IDisposable
	{
		public StringBuilder EventOrder;
		private readonly List<Guid> correlationIdList = new List<Guid>();
		private Guid lastCorrelationId;
		private readonly FrameworkElement[] ElementsToTest;
		private readonly Hyperlink[] HyperlinksToTest;
		public FocusEventOrderingTester(FrameworkElement[] elements, StringBuilder eventOrder)
		{
			this.EventOrder = eventOrder;
			this.ElementsToTest = elements;

			FrameworkElementLosingFocusHandler = new TypedEventHandler<UIElement, LosingFocusEventArgs>((source, e) =>
			{
				FrameworkElement sourceAsFrameworkElement = (FrameworkElement)source;
				EventOrder.Append($"[{sourceAsFrameworkElement.Name}LosingFocus{CanceledToLoggedString(e.Cancel)}{HandledToLoggedString(e.Handled)}{CorrelationIDToLoggedString(e.CorrelationId)}]");
			});

			FrameworkElementGettingFocusHandler = new TypedEventHandler<UIElement, GettingFocusEventArgs>((source, e) =>
			{
				FrameworkElement sourceAsFrameworkElement = (FrameworkElement)source;
				EventOrder.Append($"[{sourceAsFrameworkElement.Name}GettingFocus{CanceledToLoggedString(e.Cancel)}{HandledToLoggedString(e.Handled)}{CorrelationIDToLoggedString(e.CorrelationId)}]");
			});

			FocusManagerLosingFocusHandler = new EventHandler<Windows.UI.Xaml.Input.LosingFocusEventArgs>((source, e) =>
			{
				EventOrder.Append($"[FocusManagerLosingFocus{CanceledToLoggedString(e.Cancel)}{HandledToLoggedString(e.Handled)}{CorrelationIDToLoggedString(e.CorrelationId)}]");
			});

			FocusManagerGettingFocusHandler = new EventHandler<Windows.UI.Xaml.Input.GettingFocusEventArgs>((source, e) =>
			{
				EventOrder.Append($"[FocusManagerGettingFocus{CanceledToLoggedString(e.Cancel)}{HandledToLoggedString(e.Handled)}{CorrelationIDToLoggedString(e.CorrelationId)}]");
			});

			UIExecutor.Execute(() =>
			{
				for (int i = 0; i < ElementsToTest.Length; i++)
				{
					this.AddEventsToFrameworkElement(ElementsToTest[i]);
				}

				this.AddEventsToFocusManager();
			});
		}

		public FocusEventOrderingTester(Hyperlink[] elements, StringBuilder eventOrder)
		{
			this.EventOrder = eventOrder;
			this.HyperlinksToTest = elements;
			UIExecutor.Execute(() =>
			{
				for (int i = 0; i < HyperlinksToTest.Length; i++)
				{
					this.AddEventsToHyperlink(HyperlinksToTest[i]);
				}

				this.AddEventsToFocusManager();
			});
		}

		//Registers FocusManager event handlers.
		public FocusEventOrderingTester()
		{
			UIExecutor.Execute(() =>
			{
				this.AddEventsToFocusManager();
			});
		}

		private void AddEventsToFrameworkElement(FrameworkElement element)
		{
			element.LostFocus += this.FrameworkElementLostFocusHandler;
			element.GotFocus += this.FrameworkElementGotFocusHandler;
			element.AddHandler(
				UIElement.GettingFocusEvent,
				FrameworkElementGettingFocusHandler,
				true /*handledEventsToo*/
				);
			element.AddHandler(
				UIElement.LosingFocusEvent,
				FrameworkElementLosingFocusHandler,
				true /*handledEventsToo*/
				);
			element.NoFocusCandidateFound += this.FrameworkElementNoFocusCandidateFoundHandler;
		}

		private void AddEventsToFocusManager()
		{
			FocusManager.LosingFocus += this.FocusManagerLosingFocusHandler;
			FocusManager.GettingFocus += this.FocusManagerGettingFocusHandler;
			FocusManager.LostFocus += this.FocusManagerLostFocusHandler;
			FocusManager.GotFocus += this.FocusManagerGotFocusHandler;
		}

		private void AddEventsToHyperlink(Hyperlink element)
		{
			element.LostFocus += this.HyperlinkLostFocusHandler;
			element.GotFocus += this.HyperlinkGotFocusHandler;
		}

		private void RemoveEventsFromFrameworkElement(FrameworkElement element)
		{
			element.LostFocus -= this.FrameworkElementLostFocusHandler;
			element.GotFocus -= this.FrameworkElementGotFocusHandler;
			element.RemoveHandler(
				UIElement.GettingFocusEvent,
				FrameworkElementGettingFocusHandler);
			element.RemoveHandler(
				UIElement.LosingFocusEvent,
				FrameworkElementLosingFocusHandler);
			element.NoFocusCandidateFound -= this.FrameworkElementNoFocusCandidateFoundHandler;
		}

		private void RemoveEventsFromFocusManager()
		{
			FocusManager.LosingFocus -= this.FocusManagerLosingFocusHandler;
			FocusManager.GettingFocus -= this.FocusManagerGettingFocusHandler;
			FocusManager.LostFocus -= this.FocusManagerLostFocusHandler;
			FocusManager.GotFocus -= this.FocusManagerGotFocusHandler;
		}

		private void RemoveEventsFromHyperlink(Hyperlink element)
		{
			element.LostFocus -= this.HyperlinkLostFocusHandler;
			element.GotFocus -= this.HyperlinkGotFocusHandler;
		}

		TypedEventHandler<UIElement, GettingFocusEventArgs> FrameworkElementGettingFocusHandler;
		TypedEventHandler<UIElement, LosingFocusEventArgs> FrameworkElementLosingFocusHandler;

		EventHandler<Windows.UI.Xaml.Input.GettingFocusEventArgs> FocusManagerGettingFocusHandler;
		EventHandler<Windows.UI.Xaml.Input.LosingFocusEventArgs> FocusManagerLosingFocusHandler;

		private string CorrelationIDToLoggedString(Guid currentCorrelationId)
		{
			if (correlationIdList.Contains(currentCorrelationId))
			{
				if (currentCorrelationId == lastCorrelationId)
				{
					return "";
				}
				lastCorrelationId = currentCorrelationId;
			}
			else
			{
				correlationIdList.Add(currentCorrelationId);
				lastCorrelationId = currentCorrelationId;
			}

			int currentFocusOperationId = correlationIdList.IndexOf(currentCorrelationId) + 1;
			return (":" + currentFocusOperationId);
		}

		private string HandledToLoggedString(bool handled)
		{
			return handled ? ":Handled" : "";
		}

		private string CanceledToLoggedString(bool canceled)
		{
			return canceled ? ":Canceled" : "";
		}

		private void FrameworkElementLostFocusHandler(object source, RoutedEventArgs e)
		{
			FrameworkElement sourceAsFrameworkElement = (FrameworkElement)source;
			EventOrder.Append("[" + sourceAsFrameworkElement.Name + "LostFocus]");
		}

		private void FrameworkElementGotFocusHandler(object source, RoutedEventArgs e)
		{
			FrameworkElement sourceAsFrameworkElement = (FrameworkElement)source;
			EventOrder.Append("[" + sourceAsFrameworkElement.Name + "GotFocus]");
		}

		private void FocusManagerLostFocusHandler(object source, Windows.UI.Xaml.Input.FocusManagerLostFocusEventArgs e)
		{
			EventOrder.Append($"[FocusManagerLostFocus{CorrelationIDToLoggedString(e.CorrelationId)}]");
		}

		private void FocusManagerGotFocusHandler(object source, Windows.UI.Xaml.Input.FocusManagerGotFocusEventArgs e)
		{
			EventOrder.Append($"[FocusManagerGotFocus{CorrelationIDToLoggedString(e.CorrelationId)}]");
		}

		private void HyperlinkLostFocusHandler(object source, RoutedEventArgs e)
		{
			Hyperlink sourceAsHyperlink = (Hyperlink)source;
			EventOrder.Append("[" + sourceAsHyperlink.Name + "LostFocus]");
		}

		private void HyperlinkGotFocusHandler(object source, RoutedEventArgs e)
		{
			Hyperlink sourceAsHyperlink = (Hyperlink)source;
			EventOrder.Append("[" + sourceAsHyperlink.Name + "GotFocus]");
		}

		private void FrameworkElementNoFocusCandidateFoundHandler(object source, NoFocusCandidateFoundEventArgs e)
		{
			FrameworkElement sourceAsFrameworkElement = (FrameworkElement)source;
			EventOrder.Append("[" + sourceAsFrameworkElement.Name + "NoFocusCandidateFound]");
		}

		public void Dispose()
		{
			UIExecutor.Execute(() =>
			{
				if (ElementsToTest != null)
				{
					for (int i = 0; i < ElementsToTest.Length; i++)
					{
						this.RemoveEventsFromFrameworkElement(ElementsToTest[i]);
					}
				}
				if (HyperlinksToTest != null)
				{
					for (int i = 0; i < HyperlinksToTest.Length; i++)
					{
						this.RemoveEventsFromHyperlink(HyperlinksToTest[i]);
					}
				}
				this.RemoveEventsFromFocusManager();
			});
		}
	}
}
