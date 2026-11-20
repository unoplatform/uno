#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using OpenQA.Selenium;
using UIA = Interop.UIAutomationClient;

namespace SamplesApp.AppiumTests.Infrastructure;

internal static class WindowsSelectionReader
{
	internal static IReadOnlyList<string> GetSelectedItemNames(IWebElement element)
	{
		if (!OperatingSystem.IsWindows())
		{
			throw new PlatformNotSupportedException("UIA Selection requires the local Windows host.");
		}
		if (element is not IWrapsDriver driverElement)
		{
			throw new InvalidOperationException("The Windows element does not expose its owning WebDriver session.");
		}

		var windowElement = driverElement.WrappedDriver.FindElement(By.XPath("/*"));
		var windowHandleText = windowElement.GetAttribute("NativeWindowHandle");
		if (!long.TryParse(windowHandleText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var windowHandle) || windowHandle == 0)
		{
			throw new InvalidOperationException($"The driver-owned window has no valid NativeWindowHandle: '{windowHandleText}'.");
		}
		var processIdText = element.GetAttribute("ProcessId");
		if (!int.TryParse(processIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var processId) || processId <= 0)
		{
			throw new InvalidOperationException($"The Windows element has no valid ProcessId: '{processIdText}'.");
		}
		var automationId = element.GetAttribute("AutomationId");
		if (string.IsNullOrEmpty(automationId))
		{
			throw new InvalidOperationException("The Windows selection target must have an AutomationId.");
		}

		using var automation = new ComReference<UIA.IUIAutomation>(new UIA.CUIAutomation());
		using var window = new ComReference<UIA.IUIAutomationElement>(automation.Value.ElementFromHandle((nint)windowHandle));
		if (window.Value.CurrentProcessId != processId)
		{
			throw new InvalidOperationException("The local UIA window does not belong to the WebDriver element's process.");
		}
		using var condition = new ComReference<UIA.IUIAutomationCondition>(
			automation.Value.CreatePropertyCondition(UIA.UIA_PropertyIds.UIA_AutomationIdPropertyId, automationId));
		using var matches = new ComReference<UIA.IUIAutomationElementArray>(
			window.Value.FindAll(UIA.TreeScope.TreeScope_Subtree, condition.Value));
		if (matches.Value.Length == 0)
		{
			throw new NoSuchElementException($"UIA did not find '{automationId}' in the driver-owned window.");
		}
		if (matches.Value.Length != 1)
		{
			throw new InvalidOperationException($"AutomationId '{automationId}' is not unique in the driver-owned window.");
		}
		using var target = new ComReference<UIA.IUIAutomationElement>(matches.Value.GetElement(0));
		if (target.Value.CurrentProcessId != processId ||
			target.Value.CurrentControlType != UIA.UIA_ControlTypeIds.UIA_ComboBoxControlTypeId)
		{
			throw new InvalidOperationException("The UIA selection target does not match the Windows ComboBox.");
		}

		// A collapsed ComboBox need not expose item children or ValuePattern.
		using var selection = new ComReference<UIA.IUIAutomationSelectionPattern>(
			(UIA.IUIAutomationSelectionPattern)target.Value.GetCurrentPattern(UIA.UIA_PatternIds.UIA_SelectionPatternId));
		using var selectedItems = new ComReference<UIA.IUIAutomationElementArray>(selection.Value.GetCurrentSelection());
		var names = new List<string>(selectedItems.Value.Length);
		for (var index = 0; index < selectedItems.Value.Length; index++)
		{
			using var selected = new ComReference<UIA.IUIAutomationElement>(selectedItems.Value.GetElement(index));
			names.Add(selected.Value.CurrentName);
		}
		return names;
	}

	private sealed class ComReference<T>(T value) : IDisposable where T : class
	{
		internal T Value { get; } = value ?? throw new InvalidOperationException("UIA returned a null COM interface.");

		public void Dispose() => Marshal.ReleaseComObject(Value);
	}
}
