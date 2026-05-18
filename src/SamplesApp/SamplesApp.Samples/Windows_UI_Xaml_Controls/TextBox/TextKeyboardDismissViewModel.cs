using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.Common;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

using ICommand = System.Windows.Input.ICommand;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class TextKeyboardDismissViewModel : ViewModelBase
	{
		public TextKeyboardDismissViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			HideField = new DelegateCommand(ExecuteHideField);
		}

		public ICommand HideField { get; }

		private bool _isFieldVisible;

		public bool IsFieldVisible
		{
			get => _isFieldVisible;
			set => _isFieldVisible = value;
		}

		private void ExecuteHideField()
		{
			IsFieldVisible = !IsFieldVisible;
		}
	}
}
