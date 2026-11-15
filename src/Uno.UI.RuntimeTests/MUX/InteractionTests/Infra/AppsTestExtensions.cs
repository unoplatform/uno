// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Common/AppsTestExtensions.cs, commit fee1e8acc

using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;

using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Controls;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common
{
	public static class AppsTestExtensions
	{
		public static async Task SelectItemByName(this ComboBox comboBox, string name)
		{
			Log.Comment("ComboBox.SelectItemByName: Number of items: {0}", comboBox.AllItems.Count);
			foreach (var item in comboBox.AllItems)
			{
				Log.Comment("ComboBox.SelectItemByName: item = {0}", item.Name);
				if (item.Name == name)
				{
					Log.Comment("ComboBox.SelectItemByName: Selecting item {0}", item);
					item.Select();
					await Wait.ForIdle();
					return;
				}
			}

			throw new Exception(String.Format("ComboBox.SelectItemByName: Could not find {0}", name));
		}

		public static async Task SelectItemById(this ComboBox comboBox, string id)
		{
			Log.Comment("ComboBox.SelectItemById: Number of items: {0}", comboBox.AllItems.Count);
			foreach (var item in comboBox.AllItems)
			{
				Log.Comment("ComboBox.SelectItemById: item = {0}", item.AutomationId);
				if (item.AutomationId == id)
				{
					Log.Comment("ComboBox.SelectItemById: Selecting item {0}", item);
					item.Select();
					await Wait.ForIdle();
					return;
				}
			}

			throw new Exception(String.Format("ComboBox.SelectItemById: Could not find {0}", id));
		}

		public static async Task InvokeAndWait(this Button button, TimeSpan? timeout = null)
		{
			if (button == null)
			{
				Log.Error("Attempted to invoke a null button! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("button");
			}

			using (var waiter = button.GetInvokedWaiter())
			{
				button.Invoke();
				if (timeout == null)
				{
					await waiter.Wait();
				}
				else
				{
					await waiter.Wait(timeout.Value);
				}

			}

			await Wait.ForIdle();
		}

		public static async Task InvokeAndWait(this MenuItem menuItem, TimeSpan? timeout = null)
		{
			if (menuItem == null)
			{
				Log.Error("Attempted to invoke a null MenuItem! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("menuItem");
			}

			using (var waiter = menuItem.GetInvokedWaiter())
			{
				menuItem.Invoke();
				if (timeout == null)
				{
					await waiter.Wait();
				}
				else
				{
					await waiter.Wait(timeout.Value);
				}
			}

			await Wait.ForIdle();
		}

		public static async Task ToggleAndWait(this ToggleButton toggleButton)
		{
			if (toggleButton == null)
			{
				Log.Error("Attempted to toggle a null toggle button! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("toggleButton");
			}

			using (var waiter = toggleButton.GetToggledWaiter())
			{
				toggleButton.Toggle();
				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		public static async Task CheckAndWait(this ToggleButton toggleButton)
		{
			if (toggleButton == null)
			{
				Log.Error("Attempted to toggle a null toggle button! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("toggleButton");
			}

			if (toggleButton.ToggleState == ToggleState.Off)
			{
				using (var waiter = toggleButton.GetToggledWaiter())
				{
					toggleButton.Check();
					await waiter.Wait();
				}

				await Wait.ForIdle();
			}
		}

		public static async Task SetValueAndWait(this Edit edit, string s)
		{
			if (edit == null)
			{
				Log.Error("Attempted to set value on a null edit control! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("edit");
			}

			using (var waiter = new ValueChangedEventWaiter(edit, s))
			{
				edit.SetValue(s);
				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		public static async Task InvokeAndWait(this SplitButton splitButton)
		{
			if (splitButton == null)
			{
				Log.Error("Attempted to invoke a null split button! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("splitButton");
			}

			using (var waiter = splitButton.GetInvokedWaiter())
			{
				splitButton.Invoke();
				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		public static async Task ExpandAndWait(this SplitButton splitButton)
		{
			if (splitButton == null)
			{
				Log.Error("Attempted to expand a null split button! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("splitButton");
			}

			using (var waiter = splitButton.GetExpandedWaiter())
			{
				splitButton.Expand();
				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		public static async Task CollapseAndWait(this SplitButton splitButton)
		{
			if (splitButton == null)
			{
				Log.Error("Attempted to collapse a null split button! Dumping context...");
				DumpHelper.DumpFullContext();
				throw new ArgumentNullException("splitButton");
			}

			using (var waiter = splitButton.GetCollapsedWaiter())
			{
				splitButton.Collapse();
				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		public static string GetIdentifier(this UIObject obj)
		{
			return string.IsNullOrWhiteSpace(obj.AutomationId) ? obj.Name : obj.AutomationId;
		}

		public static string GetDescription(this UIObject obj)
		{
			List<string> patternsSupported = new List<string>();
			StringBuilder extraInformationBuilder = new StringBuilder();

			ExpandCollapseImplementation expandCollapseImplementation = new ExpandCollapseImplementation(obj);
			InvokeImplementation invokeImplementation = new InvokeImplementation(obj);
			RangeValueImplementation rangeValueImplementation = new RangeValueImplementation(obj);
			ScrollImplementation scrollImplementation = new ScrollImplementation(obj);
			TextImplementation textImplementation = new TextImplementation(obj);
			ToggleImplementation toggleImplementation = new ToggleImplementation(obj);
			ValueImplementation valueImplementation = new ValueImplementation(obj);
			WindowImplementation windowImplementation = new WindowImplementation(obj);

			// Uno: XAML automation peers have no Dock pattern.
			if (expandCollapseImplementation.IsAvailable)
			{
				patternsSupported.Add("ExpandCollapse");
				extraInformationBuilder.AppendFormat(", ExpandCollapse.ExpandCollapseState={0}", expandCollapseImplementation.ExpandCollapseState);
			}

			if (invokeImplementation.IsAvailable)
			{
				patternsSupported.Add("Invoke");
			}

			if (rangeValueImplementation.IsAvailable)
			{
				patternsSupported.Add("RangeValue");
				extraInformationBuilder.AppendFormat(", RangeValue.Minimum={0}, RangeValue.Maximum={1}, RangeValue.LargeChange={2}, RangeValue.SmallChange={3}, RangeValue.Value={4}, RangeValue.IsReadOnly={5}",
						rangeValueImplementation.Minimum, rangeValueImplementation.Maximum,
						rangeValueImplementation.LargeChange, rangeValueImplementation.SmallChange,
						rangeValueImplementation.Value,
						rangeValueImplementation.IsReadOnly);
			}

			if (scrollImplementation.IsAvailable)
			{
				patternsSupported.Add("Scroll");
				extraInformationBuilder.AppendFormat(", Scroll.HorizontallyScrollable={0}, Scroll.VerticallyScrollable={1}, Scroll.HorizontalScrollPercent={2}, Scroll.VerticalScrollPercent={3}, Scroll.HorizontalViewSize={4}, Scroll.VerticalViewSize={5}",
						scrollImplementation.HorizontallyScrollable, scrollImplementation.VerticallyScrollable,
						scrollImplementation.HorizontalScrollPercent, scrollImplementation.VerticalScrollPercent,
						scrollImplementation.HorizontalViewSize, scrollImplementation.VerticalViewSize);
			}

			if (textImplementation.IsAvailable)
			{
				patternsSupported.Add("Text");
				string textContent = textImplementation.DocumentRange.GetText(-1);
				extraInformationBuilder.AppendFormat(", Text.Text=\"{0}\", Text.SupportsTextSelection={1}", textContent.Substring(0, Math.Min(100, textContent.Length)), textImplementation.SupportsTextSelection);
			}

			if (toggleImplementation.IsAvailable)
			{
				patternsSupported.Add("Toggle");
				extraInformationBuilder.AppendFormat(", Toggle.ToggleState={0}", toggleImplementation.ToggleState);
			}

			if (valueImplementation.IsAvailable)
			{
				patternsSupported.Add("Value");
				extraInformationBuilder.AppendFormat(", Value.Value=\"{0}\", Value.IsReadOnly={1}", valueImplementation.Value, valueImplementation.IsReadOnly);
			}

			if (windowImplementation.IsAvailable)
			{
				patternsSupported.Add("Window");
				extraInformationBuilder.AppendFormat(", Window.CanMaximize={0}, Window.CanMinimize={1}, Window.IsModal={2}, Window.WindowVisualState={3}, Window.WindowInteractionState={4}, Window.IsTopmost={5}",
					windowImplementation.CanMaximize, windowImplementation.CanMinimize, windowImplementation.IsModal,
					windowImplementation.WindowVisualState, windowImplementation.WindowInteractionState, windowImplementation.IsTopmost);
			}

			string patternsSupportedString = string.Empty;

			if (patternsSupported.Count > 0)
			{
				patternsSupportedString = string.Format(", PatternsSupported={0}", string.Join(";", patternsSupported));
			}

			return string.Format("{0} [Aid={1}, Class={2}, Type={3}{4}{5}]", obj.Name, obj.AutomationId, obj.ClassName, obj.ControlType, patternsSupportedString, extraInformationBuilder.ToString());
		}

		public static string GetText(this UIObject textControl, bool shouldTrim = true)
		{
			string textControlContents = null;
			TextImplementation textImplementation = new TextImplementation(textControl);
			ValueImplementation valueImplementation = new ValueImplementation(textControl);

			if (textImplementation.IsAvailable)
			{
				textControlContents = textImplementation.DocumentRange.GetText(-1);
			}
			else if (valueImplementation.IsAvailable)
			{
				textControlContents = valueImplementation.Value;
			}
			else
			{
				Verify.Fail(string.Format("Expected the text control to implement either IValue or IText. {0} is {1}", string.IsNullOrEmpty(textControl.AutomationId) ? textControl.Name : textControl.AutomationId, textControl.GetDescription()));
			}

			// On phone, copying or pasting text causes a space to be added at the end.
			// Unless we explicitly care about that space, we'll remove it from the return value.
			if (shouldTrim)
			{
				textControlContents = textControlContents.Trim();
			}

			return textControlContents;
		}

		public static string GetTextSelection(this UIObject textControl, bool shouldTrim = true)
		{
			string textControlContents = null;
			TextImplementation textImplementation = new TextImplementation(textControl);

			if (textImplementation.IsAvailable)
			{
				textControlContents = textImplementation.SupportsTextSelection ? textImplementation.GetSelection().GetText(-1) : string.Empty;
			}
			else
			{
				Verify.Fail(string.Format("Expected the text control to implement IText. {0} is {1}", string.IsNullOrEmpty(textControl.AutomationId) ? textControl.Name : textControl.AutomationId, textControl.GetDescription()));
			}

			// On phone, copying or pasting text causes a space to be added at the end.
			// Unless we explicitly care about that space, we'll remove it from the return value.
			if (shouldTrim)
			{
				textControlContents = textControlContents.Trim();
			}

			return textControlContents;
		}
	}
}
