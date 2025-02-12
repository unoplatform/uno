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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	public sealed partial class TextBox_SelectedText : UserControl
	{
		public TextBox_SelectedText()
		{
			this.InitializeComponent();
		}

		private void btnSelection_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				txkResult.Text += $"\n Selection Start: {txbBase.SelectionStart} Length: {txbBase.SelectionLength} - {txbBase.SelectedText}";
			}
			catch (Exception ex)
			{
				txkResult.Text += $"\n Selection Start: {ex.Message}";
			}
		}

		private void btnSelectDEF_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				txbBase.SelectedText = "DEFG";
				txkResult.Text += $"\n Selection DEFG: {txbBase.SelectionStart} Length: {txbBase.SelectionLength} - {txbBase.SelectedText}";
			}
			catch (Exception ex)
			{
				txkResult.Text += $"\n Selection DEFG: {ex.Message}";
			}
		}

		private void btnSelectZXY_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				txbBase.SelectedText = "ZXY";
				txkResult.Text += $"\n Selection ZXY: {txbBase.SelectionStart} Length: {txbBase.SelectionLength} - {txbBase.SelectedText}";
			}
			catch (Exception ex)
			{
				txkResult.Text += $"\n Selection ZXY: {ex.Message}";
			}
		}

		private void txbBase_SelectionChanged(object sender, RoutedEventArgs e)
		{
			try
			{
				txkResult.Text += $"\n Start: {txbBase.SelectionStart} Length: {txbBase.SelectionLength}";
			}
			catch (Exception ex)
			{
				txkResult.Text += $"\n Start and Length: {ex.Message}";
			}
		}

		private void btnEmpty_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				txbBase.SelectedText = String.Empty;
				txkResult.Text += $"\n Selection String.Empty: {txbBase.SelectionStart} Length: {txbBase.SelectionLength} - {txbBase.SelectedText}";
			}
			catch (Exception ex)
			{
				txkResult.Text += $"\n Selection String.Empty: {ex.Message}";
			}
		}
	}
}
