using System;
using System.Collections.Generic;
using System.ComponentModel;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_xLoad_Setter : Page
	{
		public Binding_xLoad_Setter()
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
			typeof(Binding_xLoad_Setter),
			new PropertyMetadata(false, OnIsEllipseLoadedChanged));

		private static void OnIsEllipseLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Binding_xLoad_Setter self)
			{
				self.OnIsEllipseLoadedChanged((bool)e.NewValue);
			}
		}

		private void OnIsEllipseLoadedChanged(bool newValue)
		{
			IsSquareLoaded = !newValue;
		}

		private void OnEllipseLoading()
			=> UpdateStates(true);

		private void OnSquareLoading()
			=> UpdateStates(true);

		public bool IsSquareLoaded
		{
			get => (bool)GetValue(IsSquareLoadedProperty);
			set => SetValue(IsSquareLoadedProperty, value);
		}

		public static readonly DependencyProperty IsSquareLoadedProperty = DependencyProperty.Register(
			nameof(IsSquareLoaded),
			typeof(bool),
			typeof(Binding_xLoad_Setter),
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
