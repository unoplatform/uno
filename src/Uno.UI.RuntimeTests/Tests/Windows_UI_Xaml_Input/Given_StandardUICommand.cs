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
		VirtualKey.X,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.Copy,
		"Copy",
		"Copy the selected content to the clipboard",
		Symbol.Copy,
		VirtualKey.C,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.Paste,
		"Paste",
		"Insert the contents of the clipboard at the current location",
		Symbol.Paste,
		VirtualKey.V,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.SelectAll,
		"Select All",
		"Select all content",
		Symbol.SelectAll,
		VirtualKey.A,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.Delete,
		"Delete",
		"Delete the selected content",
		Symbol.Delete,
		VirtualKey.Delete,
		VirtualKeyModifiers.None)]
	[DataRow(
		StandardUICommandKind.Share,
		"Share",
		"Share the selected content",
		Symbol.Share,
		VirtualKey.None,
		VirtualKeyModifiers.None)]
	[DataRow(
		StandardUICommandKind.Save,
		"Save",
		"Save your changes",
		Symbol.Save,
		VirtualKey.S,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.Open,
		"Open",
		"Open",
		Symbol.OpenFile,
		VirtualKey.O,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.Close,
		"Close",
		"Close",
		Symbol.Cancel,
		VirtualKey.W,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.Pause,
		"Pause",
		"Pause",
		Symbol.Pause,
		VirtualKey.None,
		VirtualKeyModifiers.None)]
	[DataRow(
		StandardUICommandKind.Play,
		"Play",
		"Play",
		Symbol.Play,
		VirtualKey.None,
		VirtualKeyModifiers.None)]
	[DataRow(
		StandardUICommandKind.Stop,
		"Stop",
		"Stop",
		Symbol.Stop,
		VirtualKey.None,
		VirtualKeyModifiers.None)]
	[DataRow(
		StandardUICommandKind.Forward,
		"Forward",
		"Go to the next item",
		Symbol.Forward,
		VirtualKey.None,
		VirtualKeyModifiers.None)]
	[DataRow(
		StandardUICommandKind.Backward,
		"Backward",
		"Go to the previous item",
		Symbol.Back,
		VirtualKey.None,
		VirtualKeyModifiers.None)]
	[DataRow(
		StandardUICommandKind.Undo,
		"Undo",
		"Reverse the most recent action",
		Symbol.Undo,
		VirtualKey.Z,
		VirtualKeyModifiers.Control)]
	[DataRow(
		StandardUICommandKind.Redo,
		"Redo",
		"Repeat the most recently undone action",
		Symbol.Redo,
		VirtualKey.Y,
		VirtualKeyModifiers.Control)]
	public void When_Predefined_StandardUICommand(
		StandardUICommandKind kind,
		string label,
		string description,
		Symbol symbol,
		VirtualKey virtualKey,
		VirtualKeyModifiers modifiers)
	{
		var SUT = new StandardUICommand(kind);
		AssertStandardUICommandProperties(SUT, label, description, symbol, virtualKey, modifiers);
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
			VirtualKeyModifiers.Control);
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
			VirtualKeyModifiers.Control);

		SUT.Kind = StandardUICommandKind.Cut;

		AssertStandardUICommandProperties(
			SUT,
			"Cut",
			"Remove the selected content and put it on the clipboard",
			Symbol.Cut,
			VirtualKey.X,
			VirtualKeyModifiers.Control);
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
