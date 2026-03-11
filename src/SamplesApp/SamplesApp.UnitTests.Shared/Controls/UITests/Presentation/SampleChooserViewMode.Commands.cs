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
using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Storage;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.IO;
using Uno.Disposables;
using System.ComponentModel;
using System.Windows.Input;
using Uno.UI.Common;

#if XAMARIN || UNO_REFERENCE_API
using Microsoft.UI.Xaml.Controls;
#else
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
#endif

using ICommand = System.Windows.Input.ICommand;

namespace SampleControl.Presentation;

public partial class SampleChooserViewModel : System.ComponentModel.INotifyPropertyChanged
{
	private void InitializeCommands()
	{
		LogViewDumpCommand = new DelegateCommand(() => _ = LogViewDump(CancellationToken.None));
		ShowPreviousSectionCommand = new DelegateCommand(() => ShowPreviousSection(CancellationToken.None));
		ShowNewSectionCommand = new DelegateCommand<string>(section => ShowNewSection(CancellationToken.None, ConvertSectionEnum(section)));
		ToggleFavoriteCommand = new DelegateCommand<SampleChooserContent>(sample => _ = ToggleFavorite(CancellationToken.None, sample));
		RecordAllTestsCommand = new DelegateCommand(() => _ = RecordAllTests(CancellationToken.None));
		LoadPreviousTestCommand = new DelegateCommand(() => _ = LoadPreviousTest(CancellationToken.None)) { CanExecuteEnabled = false };
		ReloadCurrentTestCommand = new DelegateCommand(() => _ = ReloadCurrentTest(CancellationToken.None)) { CanExecuteEnabled = false };
		LoadNextTestCommand = new DelegateCommand(() => _ = LoadNextTest(CancellationToken.None)) { CanExecuteEnabled = false };
		OpenRuntimeTestsCommand = new DelegateCommand(() => _ = OpenRuntimeTests(CancellationToken.None));
		CreateNewWindowCommand = new DelegateCommand(() => CreateNewWindow());
		OpenPlaygroundCommand = new DelegateCommand(() => OpenPlayground());
		OpenHelpCommand = new DelegateCommand(() => _ = OpenHelp(CancellationToken.None));
	}

	public ICommand LogViewDumpCommand { get; private set; }
	public ICommand ShowPreviousSectionCommand { get; private set; }
	public ICommand ShowNewSectionCommand { get; private set; }
	public ICommand ToggleFavoriteCommand { get; private set; }
	public ICommand RecordAllTestsCommand { get; private set; }
	public ICommand LoadPreviousTestCommand { get; private set; }
	public ICommand ReloadCurrentTestCommand { get; private set; }
	public ICommand LoadNextTestCommand { get; private set; }
	public ICommand OpenRuntimeTestsCommand { get; private set; }
	public ICommand CreateNewWindowCommand { get; private set; }
	public ICommand OpenPlaygroundCommand { get; private set; }
	public ICommand OpenHelpCommand { get; private set; }
}
