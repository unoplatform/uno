using System.Threading.Tasks;
using System.Windows.Input;
using Private.Infrastructure;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

[TestClass]
[RunsOnUIThread]
public class Given_StandardUICommand
{
	[TestMethod]
	public void When_KeyboardAccelerators_Retrieved()
	{
		var xamlUICommand = new StandardUICommand();
		var keyboardAccelerators = xamlUICommand.KeyboardAccelerators;
		Assert.IsNotNull(keyboardAccelerators);
	}

	[TestMethod]
	public void When_CanExecute_Default()
	{
		var SUT = new StandardUICommand();
		var command = (ICommand)SUT;
		Assert.IsTrue(command.CanExecute(null));
	}

	[TestMethod]
	public void When_Execute()
	{
		bool executed = false;
		var SUT = new StandardUICommand();
		var command = (ICommand)SUT;
		SUT.ExecuteRequested += (s, e) => executed = true;
		command.Execute(null);
		Assert.IsTrue(executed);
	}

	[TestMethod]
	public void When_CanExecute_Handled()
	{
		var SUT = new StandardUICommand();
		var command = (ICommand)SUT;
		SUT.CanExecuteRequested += (sender, args) => args.CanExecute = false;
		Assert.IsFalse(command.CanExecute(null));
	}


	[TestMethod]
	public void When_CanExecute_Changed()
	{
		var SUT = new StandardUICommand();
		var command = (ICommand)SUT;
		bool raised = false;
		command.CanExecuteChanged += (sender, args) => raised = true;
		SUT.NotifyCanExecuteChanged();
		Assert.IsTrue(raised);
	}

#if HAS_UNO
	[TestMethod]
	public void When_Child_Command_CanExecute()
	{
		var SUT = new StandardUICommand();

		var parameter = "Test string";
		bool executeCalled = false;
		var childCommand = new Uno.UI.Common.DelegateCommand<object>(param => { executeCalled = true; });
		childCommand.CanExecuteEnabled = false;

		SUT.Command = childCommand;
		Assert.IsFalse(SUT.CanExecute(parameter));
		SUT.Execute(parameter);
		Assert.IsFalse(executeCalled);

		childCommand.CanExecuteEnabled = true;
		Assert.IsTrue(SUT.CanExecute(parameter));

		SUT.Execute(parameter);
		Assert.IsTrue(executeCalled);
	}
#endif

	[TestMethod]
	[DataRow(
		StandardUICommandKind.Cut,
		"Cut",
		"Remove the selected content and put it on the clipboard",
		Symbol.Cut,
		VirtualKey.X)]
	[DataRow(
		StandardUICommandKind.Copy,
		"Copy",
		"Copy the selected content to the clipboard",
		Symbol.Copy,
		VirtualKey.C)]
	[DataRow(
		StandardUICommandKind.Paste,
		"Paste",
		"Insert the contents of the clipboard at the current location",
		Symbol.Paste,
		VirtualKey.V)]
	[DataRow(
		StandardUICommandKind.SelectAll,
		"Select All",
		"Select all content",
		Symbol.SelectAll,
		VirtualKey.A)]
	[DataRow(
		StandardUICommandKind.Delete,
		"Delete",
		"Delete the selected content",
		Symbol.Delete,
		VirtualKey.Delete)]
	[DataRow(
		StandardUICommandKind.Share,
		"Share",
		"Share the selected content",
		Symbol.Share,
		VirtualKey.None)]
	[DataRow(
		StandardUICommandKind.Save,
		"Save",
		"Save your changes",
		Symbol.Save,
		VirtualKey.S)]
	[DataRow(
		StandardUICommandKind.Open,
		"Open",
		"Open",
		Symbol.OpenFile,
		VirtualKey.O)]
	[DataRow(
		StandardUICommandKind.Close,
		"Close",
		"Close",
		Symbol.Cancel,
		VirtualKey.W)]
	[DataRow(
		StandardUICommandKind.Pause,
		"Pause",
		"Pause",
		Symbol.Pause,
		VirtualKey.None)]
	[DataRow(
		StandardUICommandKind.Play,
		"Play",
		"Play",
		Symbol.Play,
		VirtualKey.None)]
	[DataRow(
		StandardUICommandKind.Stop,
		"Stop",
		"Stop",
		Symbol.Stop,
		VirtualKey.None)]
	[DataRow(
		StandardUICommandKind.Forward,
		"Forward",
		"Go to the next item",
		Symbol.Forward,
		VirtualKey.None)]
	[DataRow(
		StandardUICommandKind.Backward,
		"Backward",
		"Go to the previous item",
		Symbol.Back,
		VirtualKey.None)]
	[DataRow(
		StandardUICommandKind.Undo,
		"Undo",
		"Reverse the most recent action",
		Symbol.Undo,
		VirtualKey.Z)]
	[DataRow(
		StandardUICommandKind.Redo,
		"Redo",
		"Repeat the most recently undone action",
		Symbol.Redo,
		VirtualKey.Y)]
	public void When_Predefined_StandardUICommand(
		StandardUICommandKind kind,
		string label,
		string description,
		Symbol symbol,
		VirtualKey virtualKey)
	{
		var SUT = new StandardUICommand(kind);
		var expectedModifiers = GetExpectedModifierForKey(virtualKey);
		AssertStandardUICommandProperties(SUT, label, description, symbol, virtualKey, expectedModifiers);
	}

	/// <summary>
	/// Gets the expected modifier key based on the virtual key and platform.
	/// On macOS, iOS, and Mac Catalyst, uses VirtualKeyModifiers.Windows (which maps to the Command key) for standard shortcuts.
	/// On other platforms, uses Control key.
	/// </summary>
	private static VirtualKeyModifiers GetExpectedModifierForKey(VirtualKey virtualKey)
	{
		// Commands that use platform-specific command modifier
		var commandKeys = new[] { VirtualKey.X, VirtualKey.C, VirtualKey.V, VirtualKey.A, 
			VirtualKey.S, VirtualKey.O, VirtualKey.W, VirtualKey.Z, VirtualKey.Y };
		
		if (!commandKeys.Contains(virtualKey))
		{
			return VirtualKeyModifiers.None;
		}

#if __IOS__ || __MACCATALYST__ || __MACOS__
		return VirtualKeyModifiers.Windows; // VirtualKeyModifiers.Windows maps to Command key on Apple platforms
#else
		return VirtualKeyModifiers.Control;
#endif
	}

	[TestMethod]
	public void When_StandardUICommand_In_Xaml()
	{
		var xamlString =
"""
<StandardUICommand 
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	Kind="Copy" />
""";
		var xaml = XamlReader.Load(xamlString);
		Assert.IsInstanceOfType(xaml, typeof(StandardUICommand));
		var command = (StandardUICommand)xaml;
		AssertStandardUICommandProperties(
			command,
			"Copy",
			"Copy the selected content to the clipboard",
			Symbol.Copy,
			VirtualKey.C,
			GetExpectedModifierForKey(VirtualKey.C));
	}

	[TestMethod]
	public void When_Kind_Changes_Overrides_Default_Properties()
	{
		var SUT = new StandardUICommand();

		AssertStandardUICommandProperties(
			SUT,
			"",
			"",
			null,
			VirtualKey.None,
			VirtualKeyModifiers.None);

		SUT.Kind = StandardUICommandKind.Copy;

		AssertStandardUICommandProperties(
			SUT,
			"Copy",
			"Copy the selected content to the clipboard",
			Symbol.Copy,
			VirtualKey.C,
			GetExpectedModifierForKey(VirtualKey.C));

		SUT.Kind = StandardUICommandKind.Cut;

		AssertStandardUICommandProperties(
			SUT,
			"Cut",
			"Remove the selected content and put it on the clipboard",
			Symbol.Cut,
			VirtualKey.X,
			GetExpectedModifierForKey(VirtualKey.X));
	}

	[TestMethod]
	public void When_Kind_Changes_Does_Not_Override_Set_Properties()
	{
		var myLabel = "My label";
		var myDescription = "My description";

		var SUT = new StandardUICommand(StandardUICommandKind.Cut);
		SUT.Label = myLabel;
		SUT.Description = myDescription;

		var myAccelerator = new KeyboardAccelerator();
		myAccelerator.Key = VirtualKey.M;
		myAccelerator.Modifiers = VirtualKeyModifiers.Menu;

		SUT.KeyboardAccelerators.Clear();
		SUT.KeyboardAccelerators.Add(myAccelerator);

		AssertStandardUICommandProperties(
			SUT,
			myLabel,
			myDescription,
			Symbol.Cut,
			VirtualKey.M,
			VirtualKeyModifiers.Menu);

		SUT.Kind = StandardUICommandKind.Copy;

		AssertStandardUICommandProperties(
			SUT,
			myLabel,
			myDescription,
			Symbol.Copy,
			VirtualKey.M,
			VirtualKeyModifiers.Menu);

		var mySymbol = Symbol.Favorite;
		var mySymbolIconSource = new SymbolIconSource();
		mySymbolIconSource.Symbol = mySymbol;
		SUT.IconSource = mySymbolIconSource;

		SUT.Kind = StandardUICommandKind.SelectAll;

		AssertStandardUICommandProperties(
			SUT,
			myLabel,
			myDescription,
			mySymbol,
			VirtualKey.M,
			VirtualKeyModifiers.Menu);
	}

	private void AssertStandardUICommandProperties(
		StandardUICommand command,
		string label,
		string description,
		Symbol? symbol,
		VirtualKey virtualKey,
		VirtualKeyModifiers modifiers)
	{
		Assert.AreEqual(label, command.Label);
		Assert.AreEqual(description, command.Description);
		var iconSource = command.IconSource;
		if (symbol is null)
		{
			Assert.IsNull(iconSource);
		}
		else
		{
			Assert.IsInstanceOfType(iconSource, typeof(SymbolIconSource));
			var symbolIconSource = (SymbolIconSource)iconSource;
			Assert.AreEqual(symbol, symbolIconSource.Symbol);
		}
		if (virtualKey == VirtualKey.None)
		{
			Assert.IsEmpty(command.KeyboardAccelerators);
		}
		else
		{
			Assert.HasCount(1, command.KeyboardAccelerators);
			var keyboardAccelerator = command.KeyboardAccelerators[0];
			Assert.AreEqual(virtualKey, keyboardAccelerator.Key);
			Assert.AreEqual(modifiers, keyboardAccelerator.Modifiers);
		}
	}
}
