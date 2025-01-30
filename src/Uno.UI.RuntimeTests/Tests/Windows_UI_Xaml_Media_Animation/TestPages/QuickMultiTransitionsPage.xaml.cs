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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation.TestPages;

public sealed partial class QuickMultiTransitionsPage : Page
{
	public static class TestStateNames
	{
		public const string PhaseA = nameof(PhaseA);
		public const string PhaseB = nameof(PhaseB);
		public const string PhaseC = nameof(PhaseC);
	}

	public QuickMultiTransitionsPage()
	{
		this.InitializeComponent();
		this.Loaded += (s, e) => VisualStateManager.GoToState(this, TestStateNames.PhaseA, useTransitions: true);
	}
}
