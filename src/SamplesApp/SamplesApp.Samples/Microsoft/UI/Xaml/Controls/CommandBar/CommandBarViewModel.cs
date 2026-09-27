using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class CommandBarViewModel : ViewModelBase
	{
		private string _dynamicTitle;
		private string _dynamicSubTitle1;
		private string _dynamicSubTitle2;
		private bool _isChecked;

		public CommandBarViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			ToggleChecked = GetOrCreateCommand(() => IsChecked = !IsChecked);
			StartData();
		}

		public ICommand ToggleChecked { get; }

		private async void StartData()
		{
			var r = new Random();

			while (!CT.IsCancellationRequested)
			{
				await Task.Delay(1000);

				DynamicTitle = new string('W', r.Next(20));
				DynamicSubTitle1 = new string('A', r.Next(5));
				DynamicSubTitle2 = new string('B', r.Next(5));
			}
		}

		public string DynamicTitle
		{
			get { return _dynamicTitle; }
			set
			{
				_dynamicTitle = value; RaisePropertyChanged();
			}
		}

		public string DynamicSubTitle1
		{
			get { return _dynamicSubTitle1; }
			set
			{
				_dynamicSubTitle1 = value; RaisePropertyChanged();
			}
		}

		public string DynamicSubTitle2
		{
			get { return _dynamicSubTitle2; }
			set
			{
				_dynamicSubTitle2 = value; RaisePropertyChanged();
			}
		}

		public bool IsChecked
		{
			get { return _isChecked; }
			set
			{
				_isChecked = value;
				RaisePropertyChanged();
			}
		}
	}
}
