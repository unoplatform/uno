using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
