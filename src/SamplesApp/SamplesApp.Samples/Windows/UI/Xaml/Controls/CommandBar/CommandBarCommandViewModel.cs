using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Uno.UI.Common;
using DelegateCommand = Uno.UI.Common.DelegateCommand;

using ICommand = System.Windows.Input.ICommand;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	public class CommandBarCommandViewModel
	{
		public string Content { get; } = "Binding";
		public ICommand Command { get; } = new DelegateCommand(async () =>
		{
			await new Windows.UI.Popups.MessageDialog("Command executed").ShowAsync();
		});
	}
}
