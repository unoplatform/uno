using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using System.Windows.Input;
using Uno.UI.Common;

using ICommand = System.Windows.Input.ICommand;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class TextBoxViewModel : ViewModelBase
	{
		private const string HeaderText = "Please type:";
		private const string HeaderVisible = "Header visible";
		private const string HeaderNotVisible = "Header not visible";

		private string _myInput = "";
		private bool _isEnabled = true;
		private string _header;
		private string _placeholder;

		public TextBoxViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			ToggleHeader = new DelegateCommand(OnToggleHeader);
		}

		public ICommand MyInputEnterCommand { get; }
		public ICommand ToggleHeader { get; }

		public string MyInput
		{
			get => _myInput;
			set
			{
				_myInput = value;
				RaisePropertyChanged();
				Result = this.ToString();
			}
		}

		public bool IsEnabled
		{
			get => _isEnabled;
			set { _isEnabled = value; RaisePropertyChanged(); }
		}

		public string Header
		{
			get => _header;
			set { _header = value; RaisePropertyChanged(); }
		}

		public string Placeholder
		{
			get => _placeholder;
			set { _placeholder = value; RaisePropertyChanged(); }
		}

		private string _result;

		public string Result
		{
			get => _result;
			set { _result = value; RaisePropertyChanged(); }
		}

		private void OnToggleHeader()
		{
			if (Header == null)
			{
				Header = HeaderText;
				Placeholder = HeaderVisible;
			}
			else
			{
				Header = null;
				Placeholder = HeaderNotVisible;
			}
		}
	}
}
