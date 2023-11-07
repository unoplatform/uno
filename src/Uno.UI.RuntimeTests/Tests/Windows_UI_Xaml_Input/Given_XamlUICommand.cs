#if HAS_UNO
using Uno.UI.Common;
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
		Assert.IsTrue(SUT.CanExecute(null));
	}

	[TestMethod]
	public void When_Execute()
	{
		bool executed = false;
		var SUT = new XamlUICommand();
		SUT.ExecuteRequested += (s, e) => executed = true;
		SUT.Execute(null);
		Assert.IsTrue(executed);
	}

	[TestMethod]
	public void When_CanExecute_Handled()
	{
		var uiCommand = new XamlUICommand();
		uiCommand.CanExecuteRequested += (sender, args) => args.CanExecute = false;
		Assert.IsFalse(uiCommand.CanExecute(null));
	}


	[TestMethod]
	public void When_CanExecute_Changed()
	{
		var uiCommand = new XamlUICommand();
		bool raised = false;
		uiCommand.CanExecuteChanged += (sender, args) => raised = true;
		uiCommand.NotifyCanExecuteChanged();
		Assert.IsTrue(raised);
	}

	[TestMethod]
	public void When_Child_Command_CanExecute()
	{
		var uiCommand = new XamlUICommand();

		var parameter = "Test string";
		bool executeCalled = false;
		var childCommand = new DelegateCommand<object>(param => { executeCalled = true; });
		childCommand.CanExecuteEnabled = false;

		uiCommand.Command = childCommand;
		Assert.IsFalse(uiCommand.CanExecute(parameter));
		uiCommand.Execute(parameter);
		Assert.IsFalse(executeCalled);

		childCommand.CanExecuteEnabled = true;
		Assert.IsTrue(uiCommand.CanExecute(parameter));

		uiCommand.Execute(parameter);
		Assert.IsTrue(executeCalled);
	}
}
#endif
