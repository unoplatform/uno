// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Windows.Input;

using MUXControlsTestApp.Utilities;

using Windows.UI.Xaml.Controls;
using Common;
using System.Threading.Tasks;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class SplitButtonTests
	{
		[TestMethod]
		[Description("Verifies SplitButton default properties.")]
		public async Task VerifyDefaultsAndBasicSetting()
		{
			SplitButton splitButton = null;
			Flyout flyout = null;
			TestCommand command = null;
			int parameter = 0;

			await RunOnUIThread.Execute(() =>
			{
				flyout = new Flyout();
				command = new TestCommand();

				splitButton = new SplitButton();
				Verify.IsNotNull(splitButton);

				// Verify Defaults
				Verify.IsNull(splitButton.Flyout);
				Verify.IsNull(splitButton.Command);
				Verify.IsNull(splitButton.CommandParameter);

				// Verify basic setters
				splitButton.Flyout = flyout;
				splitButton.Command = command;
				splitButton.CommandParameter = parameter;
			});

			await RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(splitButton.Flyout, flyout);
				Verify.AreEqual(splitButton.Command, command);
				Verify.AreEqual(splitButton.CommandParameter, parameter);
			});
		}
	}

	// CanExecuteChanged is never used -- that's ok, disable the compiler warning.
#pragma warning disable CS0067
	public class TestCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;

		public TestCommand()
		{
		}

		public bool CanExecute(object o)
		{
			return true;
		}

		public void Execute(object o) { }
	}
#pragma warning restore CS0067
}
