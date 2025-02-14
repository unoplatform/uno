using System.Windows.Input;
using Windows.UI.Xaml.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

[TestClass]
[RunsOnUIThread]
public class Given_XamlUICommand
{
	[TestMethod]
	public void When_KeyboardAccelerators_Retrieved()
	{
		var xamlUICommand = new XamlUICommand();
		var keyboardAccelerators = xamlUICommand.KeyboardAccelerators;
		Assert.IsNotNull(keyboardAccelerators);
	}

	[TestMethod]
	public void When_CanExecute_Default()
	{
		var SUT = new XamlUICommand();
		var command = (ICommand)SUT;
		Assert.IsTrue(command.CanExecute(null));
	}

	[TestMethod]
	public void When_Execute()
	{
		bool executed = false;
		var SUT = new XamlUICommand();
		var command = (ICommand)SUT;
		SUT.ExecuteRequested += (s, e) => executed = true;
		command.Execute(null);
		Assert.IsTrue(executed);
	}

	[TestMethod]
	public void When_CanExecute_Handled()
	{
		var SUT = new XamlUICommand();
		var command = (ICommand)SUT;
		SUT.CanExecuteRequested += (sender, args) => args.CanExecute = false;
		Assert.IsFalse(command.CanExecute(null));
	}


	[TestMethod]
	public void When_CanExecute_Changed()
	{
		var SUT = new XamlUICommand();
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
		var SUT = new XamlUICommand();

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
}
