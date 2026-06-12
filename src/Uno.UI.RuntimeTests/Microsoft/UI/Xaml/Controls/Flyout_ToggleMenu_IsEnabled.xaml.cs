using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.RuntimeTests.Helpers;

namespace RuntimeTests.Windows_UI_Xaml_Controls.Flyout;

public sealed partial class Flyout_ToggleMenu_IsEnabled : Page
{
	public Flyout_ToggleMenu_IsEnabled()
	{
		this.InitializeComponent();
	}

	internal Flyout_ToggleMenu_IsEnabledViewModel ViewModel { get; } = new Flyout_ToggleMenu_IsEnabledViewModel();
}

internal class Flyout_ToggleMenu_IsEnabledViewModel : ViewModelBase
{
	private bool _isChecked1;
	private bool _isChecked2;
	private bool _isChecked3;
	private bool _areItemsEnabled;


	public Flyout_ToggleMenu_IsEnabledViewModel()
	{
	}

	public bool IsChecked1
	{
		get => _isChecked1;
		set
		{
			_isChecked1 = value;
			RaisePropertyChanged();
		}
	}

	public bool IsChecked2
	{
		get => _isChecked2;
		set
		{
			_isChecked2 = value;
			RaisePropertyChanged();
		}
	}

	public bool IsChecked3
	{
		get => _isChecked3;
		set
		{
			_isChecked3 = value;
			RaisePropertyChanged();
		}
	}

	public bool AreItemsEnabled
	{
		get => _areItemsEnabled;
		set
		{
			_areItemsEnabled = value;
			RaisePropertyChanged();
		}
	}
}
