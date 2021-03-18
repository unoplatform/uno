using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Uno.Logging;
using Uno.UI.Samples.UITests.Helpers;
using System.Windows.Input;
using Windows.UI.Core;
using System.Runtime.CompilerServices;

using ICommand = System.Windows.Input.ICommand;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	public class ButtonTestsViewModel : ViewModelBase
	{
		private int _myData;
		private string _message = string.Empty;
		private int comboBoxSimple_SelectedItem = 1;
		private int[] comboBoxSimple_ItemsSource = new[] { 1, 2, 3 };

		public ButtonTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			StartData();

			((Command)DisableCommand).ManualCanExecute = false;
		}

		private async void StartData()
		{
			var alternateEnableCommand = (Command)AlternateEnableCommand;

			while (!CT.IsCancellationRequested)
			{
				await Task.Delay(1000);
				MyData += 1;

				alternateEnableCommand.ManualCanExecute = MyData % 2 == 0;
			}
		}

		public int MyData
		{
			get => _myData;
			set
			{
				_myData = value;
				RaisePropertyChanged();
			}
		}

		public string Message
		{
			get => _message;
			set
			{
				_message = value;
				RaisePropertyChanged();
			}
		}

		public int ComboBoxSimple_SelectedItem
		{
			get => comboBoxSimple_SelectedItem;
			set
			{
				comboBoxSimple_SelectedItem = value;
				RaisePropertyChanged();
			}
		}

		public int[] ComboBoxSimple_ItemsSource
		{
			get => comboBoxSimple_ItemsSource;
			private set
			{
				comboBoxSimple_ItemsSource = value;
				RaisePropertyChanged();
			}
		}

		public ICommand ComboBoxSimple_GenerateNewList => GetOrCreateCommand(() =>
		{
			ComboBoxSimple_ItemsSource = ComboBoxSimple_ItemsSource.Select(x => x + 1).ToArray();
		});

		public ICommand SampleCommand => MsgCommand();

		public ICommand SampleCommand2 => MsgCommand();

		public ICommand AlternateEnableCommand => MsgCommand();

		public ICommand DisableCommand => MsgCommand();

		public ICommand CheckBox01_Command => MsgCommand();
		public ICommand CheckBox02_Command => MsgCommand();
		public ICommand CheckBox03_Command => MsgCommand();

		public ICommand OuterCommand => MsgCommand();
		public ICommand InnerCommand => MsgCommand();

		public ICommand ComboBoxSimple_Command => GetOrCreateCommand<int>(i => WriteMsg($"ComboBoxSimple_Command - {i}"));

		private ICommand MsgCommand([CallerMemberName] string commandName = null) => GetOrCreateCommand(() => WriteMsg(commandName), commandName);

		private void DoStuff()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Do Stuff");
			}

			WriteMsg("SampleCommand !" + DateTime.Now);
		}

		private void DoOtherStuff()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Do Other Stuff");
			}

			WriteMsg("SampleCommand 2 !" + DateTime.Now);
		}

		private void WriteMsg(string m)
		{
			Message = $"{Message}\n{DateTime.Now}: {m}";
		}
	}
}
