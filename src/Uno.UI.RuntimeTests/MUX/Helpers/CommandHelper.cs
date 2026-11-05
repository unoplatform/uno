// MUX Reference CommandHelper.h, tag main

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Common;

internal sealed class MenuCommand : ICommand
{
	private Action<object> _executeDelegate;
	private string _expectedParam;

	public MenuCommand(Action<object> executeDelegate, bool canExecuteFlag, string expectedParam)
	{
		_executeDelegate = executeDelegate;
		CanExecuteFlag = canExecuteFlag;
		_expectedParam = expectedParam;
	}

#pragma warning disable CS0067 // The event is never used
	public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067 // The event is never used

	public void Execute(object parameter)
	{
		LOG_OUTPUT("MenuCommand: Invoke Execute()!");
		VERIFY_IS_TRUE(_expectedParam == parameter.ToString());

		_executeDelegate(parameter);
	}

	public bool CanExecute(object parameter) => CanExecuteFlag;

	public bool CanExecuteFlag { get; set; }
}

internal static class CommandHelper
{
	internal static async Task ValidateSettingUICommandSetsProperties<TControl>(
		DependencyProperty commandProperty,
		DependencyProperty labelProperty,
		DependencyProperty iconProperty)
		where TControl : Control, new()
	{
		await RunOnUIThread(() =>
		{
			var control = new TControl();

			LOG_OUTPUT("Assigning the command to the control. We expect the control to pick up the command's properties.");
			ValidateSettingUICommandSetsProperties_SetCommand(commandProperty, control);
			ValidateSettingUICommandSetsProperties_VerifySetProperties(labelProperty, iconProperty, control);

			LOG_OUTPUT("Clearing the control's command. We expect the control to clear the properties that were set.");
			ValidateSettingUICommandSetsProperties_ClearCommand(commandProperty, control);
			ValidateSettingUICommandSetsProperties_VerifyClearedProperties(labelProperty, iconProperty, control);
		});
	}

	private static void ValidateSettingUICommandSetsProperties_SetCommand(
		DependencyProperty commandProperty,
		Control control)
	{
		string labelText = "Label";
		Symbol expectedSymbol = Symbol.Favorite;
		VirtualKey expectedKey = VirtualKey.F;
		VirtualKeyModifiers expectedKeyModifiers = VirtualKeyModifiers.Control;
		KeyboardAccelerator keyboardAccelerator = new();
		keyboardAccelerator.Key = expectedKey;
		keyboardAccelerator.Modifiers = expectedKeyModifiers;
		string accessKeyText = "F";
		string descriptionText = "Description";

		SymbolIconSource symbolIconSource = new();
		symbolIconSource.Symbol = expectedSymbol;

		XamlUICommand uiCommand = new();
		uiCommand.Label = labelText;
		uiCommand.IconSource = symbolIconSource;
		uiCommand.KeyboardAccelerators.Add(keyboardAccelerator);
		uiCommand.AccessKey = accessKeyText;
		uiCommand.Description = descriptionText;

		control.SetValue(commandProperty, uiCommand);
	}

	private static void ValidateSettingUICommandSetsProperties_VerifySetProperties(
		DependencyProperty labelProperty,
		DependencyProperty iconProperty,
		Control control)
	{
		string labelText = "Label";
		Symbol expectedSymbol = Symbol.Favorite;
		VirtualKey expectedKey = VirtualKey.F;
		VirtualKeyModifiers expectedKeyModifiers = VirtualKeyModifiers.Control;
		string accessKeyText = "F";
		string descriptionText = "Description";

		if (labelProperty != null)
		{
			var controlLabelText = (string)control.GetValue(labelProperty);

			LOG_OUTPUT($"Expected content text: \"{labelText}\"");
			LOG_OUTPUT($"Actual content text: \"{controlLabelText}\"");
			VERIFY_IS_TRUE(string.CompareOrdinal(labelText, controlLabelText) == 0);
		}

		if (iconProperty != null)
		{
			var iconSourceElement = (IconSourceElement)control.GetValue(iconProperty);
			VERIFY_IS_NOT_NULL(iconSourceElement);

			var symbolIconSource = (SymbolIconSource)iconSourceElement.IconSource;
			VERIFY_IS_NOT_NULL(symbolIconSource);
			VERIFY_ARE_EQUAL(expectedSymbol, symbolIconSource.Symbol);
		}

		VERIFY_ARE_EQUAL(1, control.KeyboardAccelerators.Count);
		KeyboardAccelerator actualKeyboardAccelerator = control.KeyboardAccelerators[0];

		LOG_OUTPUT($"Expected keyboard accelerator key: {(int)expectedKey}");
		LOG_OUTPUT($"Actual keyboard accelerator key: {(int)actualKeyboardAccelerator.Key}");
		VERIFY_ARE_EQUAL(expectedKey, actualKeyboardAccelerator.Key);
		LOG_OUTPUT($"Expected keyboard accelerator key modifiers: {(int)expectedKeyModifiers}");
		LOG_OUTPUT($"Actual keyboard accelerator key modifiers: {(int)actualKeyboardAccelerator.Modifiers}");
		VERIFY_ARE_EQUAL(expectedKeyModifiers, actualKeyboardAccelerator.Modifiers);

		LOG_OUTPUT($"Expected access key: \"{accessKeyText}\"");
		LOG_OUTPUT($"Actual access key: \"{control.AccessKey}\"");
		VERIFY_IS_TRUE(string.CompareOrdinal(accessKeyText, control.AccessKey) == 0);

		string helpText = AutomationProperties.GetHelpText(control);
		LOG_OUTPUT($"Expected help text: \"{descriptionText}\"");
		LOG_OUTPUT($"Actual help text: \"{helpText}\"");
		VERIFY_IS_TRUE(string.CompareOrdinal(descriptionText, helpText) == 0);

		var toolTipText = (string)ToolTipService.GetToolTip(control);
		VERIFY_IS_NOT_NULL(toolTipText);

		LOG_OUTPUT($"Expected tool tip text: \"{descriptionText}\"");
		LOG_OUTPUT($"Actual tool tip text: \"{toolTipText}\"");
		VERIFY_IS_TRUE(string.CompareOrdinal(descriptionText, toolTipText) == 0);
	}

	private static void ValidateSettingUICommandSetsProperties_ClearCommand(
		DependencyProperty commandProperty,
		Control control)
	{
		control.SetValue(commandProperty, null);
	}

	private static void ValidateSettingUICommandSetsProperties_VerifyClearedProperties(
		DependencyProperty labelProperty,
		DependencyProperty iconProperty,
		Control control)
	{
		if (labelProperty != null)
		{
			var controlLabelText = (string)control.GetValue(labelProperty);

			LOG_OUTPUT("Expected content text: \"\"");
			LOG_OUTPUT($"Actual content text: \"{controlLabelText}\"");
			VERIFY_IS_TRUE(string.CompareOrdinal("", controlLabelText) == 0);
		}

		if (iconProperty != null)
		{
			var iconSourceElement = (IconSourceElement)control.GetValue(iconProperty);

			VERIFY_IS_NULL(iconSourceElement);
		}

		VERIFY_ARE_EQUAL(0, control.KeyboardAccelerators.Count);

		LOG_OUTPUT("Expected access key: \"\"");
		LOG_OUTPUT($"Actual access key: \"{control.AccessKey}\"");
		// TODO Uno: AccessKey and AutomationProperties.HelpText default to null, where WinUI's default is empty.
		VERIFY_IS_TRUE(string.CompareOrdinal("", control.AccessKey ?? "") == 0);

		string helpText = AutomationProperties.GetHelpText(control);
		LOG_OUTPUT("Expected help text: \"\"");
		LOG_OUTPUT($"Actual help text: \"{helpText}\"");
		VERIFY_IS_TRUE(string.CompareOrdinal("", helpText ?? "") == 0);

		VERIFY_IS_NULL(ToolTipService.GetToolTip(control));
	}

	internal static async Task ValidateSettingUICommandDoesNotOverwriteProperties<TControl>(
		DependencyProperty commandProperty,
		DependencyProperty labelProperty,
		DependencyProperty iconProperty)
		where TControl : Control, new()
	{
		await RunOnUIThread(() =>
		{
			string controlLabelText = "Control label";
			string commandLabelText = "Label";
			Symbol controlSymbol = Symbol.OpenFile;
			Symbol commandSymbol = Symbol.Favorite;
			VirtualKey controlKey = VirtualKey.A;
			VirtualKeyModifiers controlKeyModifiers = VirtualKeyModifiers.Menu;
			KeyboardAccelerator controlKeyboardAccelerator = new();
			controlKeyboardAccelerator.Key = controlKey;
			controlKeyboardAccelerator.Modifiers = controlKeyModifiers;
			VirtualKey commandKey = VirtualKey.F;
			VirtualKeyModifiers commandKeyModifiers = VirtualKeyModifiers.Control;
			KeyboardAccelerator commandKeyboardAccelerator = new();
			commandKeyboardAccelerator.Key = commandKey;
			commandKeyboardAccelerator.Modifiers = commandKeyModifiers;
			string controlAccessKey = "A";
			string commandAccessKey = "F";
			string controlHelpText = "Control help text";
			string controlToolTipText = "Control tool tip text";
			string commandDescriptionText = "Command description";

			SymbolIcon controlSymbolIcon = new();
			controlSymbolIcon.Symbol = controlSymbol;
			SymbolIconSource commandSymbolIconSource = new();
			commandSymbolIconSource.Symbol = commandSymbol;

			var control = new TControl();

			if (labelProperty != null)
			{
				control.SetValue(labelProperty, controlLabelText);
			}

			if (iconProperty != null)
			{
				control.SetValue(iconProperty, controlSymbolIcon);
			}

			control.KeyboardAccelerators.Add(controlKeyboardAccelerator);
			control.AccessKey = controlAccessKey;
			AutomationProperties.SetHelpText(control, controlHelpText);
			ToolTipService.SetToolTip(control, controlToolTipText);

			XamlUICommand uiCommand = new();
			uiCommand.Label = commandLabelText;
			uiCommand.IconSource = commandSymbolIconSource;
			uiCommand.KeyboardAccelerators.Add(commandKeyboardAccelerator);
			uiCommand.AccessKey = commandAccessKey;
			uiCommand.Description = commandDescriptionText;
			control.SetValue(commandProperty, uiCommand);

			if (labelProperty != null)
			{
				var actualControlLabelText = (string)control.GetValue(labelProperty);

				LOG_OUTPUT($"Expected content text: \"{controlLabelText}\"");
				LOG_OUTPUT($"Actual content text: \"{actualControlLabelText}\"");
				VERIFY_IS_TRUE(string.CompareOrdinal(controlLabelText, actualControlLabelText) == 0);
			}

			if (iconProperty != null)
			{
				var symbolIcon = (SymbolIcon)control.GetValue(iconProperty);

				VERIFY_IS_NOT_NULL(symbolIcon);
				VERIFY_ARE_EQUAL(controlSymbol, symbolIcon.Symbol);
			}

			VERIFY_ARE_EQUAL(1, control.KeyboardAccelerators.Count);
			KeyboardAccelerator actualKeyboardAccelerator = control.KeyboardAccelerators[0];

			LOG_OUTPUT($"Expected keyboard accelerator key: {(int)controlKey}");
			LOG_OUTPUT($"Actual keyboard accelerator key: {(int)actualKeyboardAccelerator.Key}");
			VERIFY_ARE_EQUAL(controlKey, actualKeyboardAccelerator.Key);
			LOG_OUTPUT($"Expected keyboard accelerator key modifiers: {(int)controlKeyModifiers}");
			LOG_OUTPUT($"Actual keyboard accelerator key modifiers: {(int)actualKeyboardAccelerator.Modifiers}");
			VERIFY_ARE_EQUAL(controlKeyModifiers, actualKeyboardAccelerator.Modifiers);

			LOG_OUTPUT($"Expected access key: \"{controlAccessKey}\"");
			LOG_OUTPUT($"Actual access key: \"{control.AccessKey}\"");
			VERIFY_IS_TRUE(string.CompareOrdinal(controlAccessKey, control.AccessKey) == 0);

			string actualHelpText = AutomationProperties.GetHelpText(control);
			LOG_OUTPUT($"Expected help text: \"{controlHelpText}\"");
			LOG_OUTPUT($"Actual help text: \"{actualHelpText}\"");
			VERIFY_IS_TRUE(string.CompareOrdinal(controlHelpText, actualHelpText) == 0);

			var actualToolTipText = (string)ToolTipService.GetToolTip(control);
			VERIFY_IS_NOT_NULL(actualToolTipText);

			LOG_OUTPUT($"Expected tool tip text: \"{controlToolTipText}\"");
			LOG_OUTPUT($"Actual tool tip text: \"{actualToolTipText}\"");
			VERIFY_IS_TRUE(string.CompareOrdinal(controlToolTipText, actualToolTipText) == 0);
		});
	}
}
