using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample]
	public sealed partial class ComboBox_RemoveSelectedIndex : UserControl
    {
        public ComboBox_RemoveSelectedIndex()
        {
            this.InitializeComponent();
			steps.TextWrapping = TextWrapping.Wrap;
		}

		private void comboChanged_Selection(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				var i = combo.SelectedIndex;
				if (i >= 0)
				{
					var text = ((ComboBoxItem)combo.SelectedItem).Content.ToString();
					steps.Text += $"\nSelected selectedIndex: {i} - {text}";
				}
				else
				{
					steps.Text += $"\nSelected selectedIndex: {i}";
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var i = combo.SelectedIndex;
				var text = ((ComboBoxItem)combo.SelectedItem).Content.ToString();
				combo.Items.RemoveAt(combo.SelectedIndex);
				steps.Text += $"\nRemoved selectedIndex: {i} - {text}";
			}
			catch (Exception ex)
			{
				steps.Text += "\nError: " + ex.Message;
			}
		}

		private void btnRemoveTwoItems_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				combo.Items.RemoveAt(2);
				steps.Text += $"\nRemoved Index: 2";
				combo.Items.RemoveAt(3);
				steps.Text += $"\nRemoved Index: 3";
			}
			catch (Exception ex)
			{
				steps.Text += "\nError: " + ex.Message;
			}
		}

		private void btnRemoveItemsThreeAndTwo_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				combo.Items.RemoveAt(3);
				steps.Text += $"\nRemoved Index: 3";
				combo.Items.RemoveAt(2);
				steps.Text += $"\nRemoved Index: 2";
			}
			catch (Exception ex)
			{
				steps.Text += "\nError: " + ex.Message;
			}
		}

		private void btnRemoveIndexZero_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				combo.Items.RemoveAt(0);
				steps.Text += $"\nRemoved Index: 0";
			}
			catch (Exception ex)
			{
				steps.Text += "\nError: " + ex.Message;
			}
		}
	}
}
