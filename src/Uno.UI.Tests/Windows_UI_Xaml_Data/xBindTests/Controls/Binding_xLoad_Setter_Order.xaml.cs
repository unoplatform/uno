using System;
using System.Collections.Generic;
using System.ComponentModel;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_xLoad_Setter_Order : Page
	{
		public Binding_xLoad_Setter_Order()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
		}

		public bool IsEllipseLoaded
		{
			get => (bool)GetValue(IsEllipseLoadedProperty);
			set => SetValue(IsEllipseLoadedProperty, value);
		}

		public static readonly DependencyProperty IsEllipseLoadedProperty = DependencyProperty.Register(
			nameof(IsEllipseLoaded),
			typeof(bool),
			typeof(Binding_xLoad_Setter_Order),
			new PropertyMetadata(false, OnIsEllipseLoadedChanged));

		private static void OnIsEllipseLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Binding_xLoad_Setter_Order self)
			{
				self.OnIsEllipseLoadedChanged((bool)e.NewValue);
			}
		}

		private void OnIsEllipseLoadedChanged(bool newValue)
		{
			IsSquareLoaded = !newValue;
			UpdateStates(true);
		}

		public bool IsSquareLoaded
		{
			get => (bool)GetValue(IsSquareLoadedProperty);
			set => SetValue(IsSquareLoadedProperty, value);
		}

		public static readonly DependencyProperty IsSquareLoadedProperty = DependencyProperty.Register(
			nameof(IsSquareLoaded),
			typeof(bool),
			typeof(Binding_xLoad_Setter_Order),
			new PropertyMetadata(true));

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			UpdateStates(false);
		}

		private void UpdateStates(bool useTransitions)
		{
			if (this.IsEllipseLoaded)
			{
				VisualStateManager.GoToState(this, this.EllipseState.Name, useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, this.SquareState.Name, useTransitions);
			}
		}
	}
}
