// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using System.Reflection;

namespace MUXControlsTestApp.Utilities
{
	// Test control intended to help you see all the different visual states your control can be in
	public sealed partial class ControlStateViewer : UserControl
	{
		Type _controlType;
		List<string> _states;

		public ControlStateViewer()
		{
			this.InitializeComponent();
		}

		public Type ControlType
		{
			get
			{
				return _controlType;
			}

			set
			{
				_controlType = value;
				UpdateGrid();
			}
		}

		public List<string> States
		{
			get
			{
				return _states;
			}

			set
			{
				_states = value;
				UpdateGrid();
			}
		}

		private void UpdateGrid()
		{
			StateGridView.Items.Clear();

			if (_controlType == null || _states == null)
			{
				return;
			}

			foreach (string state in _states)
			{
				StackPanel sp = new StackPanel();
				sp.Margin = new Thickness(4);

				TextBlock textBlock = new TextBlock();
				textBlock.Text = state;
				textBlock.FontSize = 9;
				sp.Children.Add(textBlock);

				Control c = Activator.CreateInstance(_controlType) as Control;
				c.Loaded += Control_Loaded;
				c.DataContext = state;

				ContentControl cc = c as ContentControl;
				if (cc != null)
				{
					string[] s = _controlType.ToString().Split('.');
					cc.Content = s[s.Length - 1];
				}

				sp.Children.Add(c);

				StateGridView.Items.Add(sp);
			}
		}

		private void Control_Loaded(object sender, RoutedEventArgs e)
		{
			Control c = sender as Control;
			if (c != null)
			{
				VisualStateManager.GoToState(c, c.DataContext as string, false);
			}
		}
	}
}
