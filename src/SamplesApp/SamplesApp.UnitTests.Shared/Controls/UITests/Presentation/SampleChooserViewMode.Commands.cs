using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SampleControl.Entities;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Entities;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Globalization;
using Windows.UI.Xaml.Data;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Storage;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml;
using System.IO;
using Uno.Disposables;
using System.ComponentModel;
using System.Windows.Input;
using Uno.UI.Common;

#if XAMARIN || UNO_REFERENCE_API
using Windows.UI.Xaml.Controls;
#else
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Controls;
#endif

using ICommand = System.Windows.Input.ICommand;

namespace SampleControl.Presentation
{
	public partial class SampleChooserViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		private void InitializeCommands()
		{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			LogViewDumpCommand = new DelegateCommand(() => LogViewDump(CancellationToken.None));
			ShowPreviousSectionCommand = new DelegateCommand(() => ShowPreviousSection(CancellationToken.None));
			ShowNewSectionCommand = new DelegateCommand<string>(section => ShowNewSection(CancellationToken.None, ConvertSectionEnum(section)));
			ToggleFavoriteCommand = new DelegateCommand<SampleChooserContent>(sample => ToggleFavorite(CancellationToken.None, sample));
			RecordAllTestsCommandCommand = new DelegateCommand(() => RecordAllTests(CancellationToken.None));
			LoadPreviousTestCommand = new DelegateCommand(() => LoadPreviousTest(CancellationToken.None)) { CanExecuteEnabled = false };
			ReloadCurrentTestCommand = new DelegateCommand(() => ReloadCurrentTest(CancellationToken.None)) { CanExecuteEnabled = false };
			LoadNextTestCommand = new DelegateCommand(() => LoadNextTest(CancellationToken.None)) { CanExecuteEnabled = false };
			ShowTestInformationCommand = new DelegateCommand(() => ShowTestInformation(CancellationToken.None)) { CanExecuteEnabled = false };

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		public ICommand LogViewDumpCommand { get; private set; }
		public ICommand ShowPreviousSectionCommand { get; private set; }
		public ICommand ShowNewSectionCommand { get; private set; }
		public ICommand ToggleFavoriteCommand { get; private set; }
		public ICommand RecordAllTestsCommandCommand { get; private set; }
		public ICommand LoadPreviousTestCommand { get; private set; }
		public ICommand ReloadCurrentTestCommand { get; private set; }
		public ICommand LoadNextTestCommand { get; private set; }
		public ICommand ShowTestInformationCommand { get; private set; }
	}
}
