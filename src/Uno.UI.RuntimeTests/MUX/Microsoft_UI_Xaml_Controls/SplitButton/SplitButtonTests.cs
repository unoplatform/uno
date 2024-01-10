// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX commit reference 36f8f8f6d5f11f414fdaa0462d0c4cb845cf4254

#if !WINDOWS_UWP
using System;
using System.Windows.Input;

using MUXControlsTestApp.Utilities;

using Microsoft.UI.Xaml.Controls;
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

using SplitButton = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SplitButton;
using ToggleSplitButton = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ToggleSplitButton;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
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

			await RunOnUIThread.ExecuteAsync(() =>
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

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Verify.AreEqual(splitButton.Flyout, flyout);
				Verify.AreEqual(splitButton.Command, command);
				Verify.AreEqual(splitButton.CommandParameter, parameter);
			});
		}

		[TestMethod]
		[Description("Verifies ToggleSplitButton IsChecked property.")]
		public void VerifyIsCheckedProperty()
		{
			RunOnUIThread.Execute(() =>
			{
				ToggleSplitButton toggleSplitButton = new ToggleSplitButton();

				Verify.IsFalse(toggleSplitButton.IsChecked, "ToggleSplitButton is not unchecked");

				toggleSplitButton.SetValue(ToggleSplitButton.IsCheckedProperty, true);

				bool isChecked = (bool)toggleSplitButton.GetValue(ToggleSplitButton.IsCheckedProperty);
				Verify.IsTrue(isChecked, "ToggleSplitButton is not checked");
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public void VerifyFontFamilyForChevron()
		{
			using (StyleHelper.UseFluentStyles())
			{
				var splitButton = new SplitButton();
				TestServices.WindowHelper.WindowContent = splitButton;

				var secondayButton = splitButton.GetTemplateChild("SecondaryButton");
				var font = ((secondayButton as Button).Content as TextBlock).FontFamily;
				Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
			}
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
#endif
