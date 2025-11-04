using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;

namespace MUXControlsTestApp;

public sealed partial class TitleBarPageWindow : Window
{
	private int backRequestedCount = 0;
	private int paneToggleRequestedCount = 0;

	private const int GWL_EXSTYLE = -20;
	private const int WS_EX_LAYOUTRTL = 0x00400000;

	public TitleBarPageWindow()
	{
		this.InitializeComponent();

		// C# code to set AppTitleBar uielement as titlebar.
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.WindowingTitleBar);

		// Set titlebar's title to window's title.
		this.Title = this.WindowingTitleBar.Title;
	}

	private void CmbTitleBarOutputDebugStringLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		//MUXControlsTestHooks.SetOutputDebugStringLevelForType(
		//	"TitleBar",
		//	cmbTitleBarOutputDebugStringLevel.SelectedIndex == 1 || cmbTitleBarOutputDebugStringLevel.SelectedIndex == 2,
		//	cmbTitleBarOutputDebugStringLevel.SelectedIndex == 2);
	}

	private void EnableRTLToggleButton_Checked(object sender, RoutedEventArgs e)
	{
		TitleBarPageWindowGrid.FlowDirection = FlowDirection.RightToLeft;
		UpdateCaptionButtonDirection(TitleBarPageWindowGrid.FlowDirection);
	}

	private void EnableRTLToggleButton_Unchecked(object sender, RoutedEventArgs e)
	{
		TitleBarPageWindowGrid.FlowDirection = FlowDirection.LeftToRight;
		UpdateCaptionButtonDirection(TitleBarPageWindowGrid.FlowDirection);
	}

	private static nint GetWindowHandleForCurrentWindow(object target) =>
		WinRT.Interop.WindowNative.GetWindowHandle(target);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
	internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, nint newProc);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
	internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

	private void UpdateCaptionButtonDirection(FlowDirection direction)
	{
		var hwnd = GetWindowHandleForCurrentWindow(this);

		if (hwnd != 0)
		{
			var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);

			if (direction == FlowDirection.RightToLeft)
			{
				exStyle |= WS_EX_LAYOUTRTL;
			}
			else
			{
				exStyle &= ~WS_EX_LAYOUTRTL;
			}

			SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);
		}
	}

	private void WindowingTitleBar_BackRequested(TitleBar sender, object args)
	{
		BackRequestedCountTextBox.Text = (backRequestedCount++).ToString();
	}

	private void IsBackButtonVisibleCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			WindowingTitleBar.IsBackButtonVisible = IsBackButtonVisibleCheckBox.IsChecked.Value;
		}
	}

	private void IsBackButtonEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			WindowingTitleBar.IsBackButtonEnabled = IsBackButtonEnabledCheckBox.IsChecked.Value;
		}
	}

	private void WindowingTitleBar_PaneToggleRequested(TitleBar sender, object args)
	{
		PaneToggleButtonRequestedCountTextBox.Text = (paneToggleRequestedCount++).ToString();
	}

	private void IsPaneToggleButtonVisibleCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			WindowingTitleBar.IsPaneToggleButtonVisible = IsPaneToggleButtonVisibleCheckbox.IsChecked.Value;
		}
	}

	private void SetIconCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			if (SetIconCheckBox.IsChecked.Value)
			{
				var icon = new Microsoft.UI.Xaml.Controls.SymbolIconSource();
				icon.Symbol = Symbol.Mail;
				WindowingTitleBar.IconSource = icon;
			}
			else
			{
				WindowingTitleBar.IconSource = null;
			}
		}
	}

	private void CustomContentCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			if (CustomContentCheckBox.IsChecked.Value)
			{
				string xaml =
				@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                         <Grid.ColumnDefinitions>
                            <ColumnDefinition Width='Auto'/>
                            <ColumnDefinition Width='*' />
                         </Grid.ColumnDefinitions >

                         <Button Content='Left'/>
                         <Button Grid.Column='1' Content='Right' HorizontalAlignment='Right'/>
                    </Grid>";

				var element = (Grid)XamlReader.Load(xaml);
				WindowingTitleBar.Content = element;
			}
			else
			{
				WindowingTitleBar.Content = null;
			}
		}
	}

	private void LeftHeaderCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			if (LeftHeaderCheckBox.IsChecked.Value)
			{
				var button = new Button();
				button.Content = "LeftHeader";
				WindowingTitleBar.LeftHeader = button;
			}
			else
			{
				WindowingTitleBar.LeftHeader = null;
			}
		}
	}

	private void RightHeaderCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			if (RightHeaderCheckBox.IsChecked.Value)
			{
				var button = new Button();
				button.Content = "RightHeader";
				WindowingTitleBar.RightHeader = button;
			}
			else
			{
				WindowingTitleBar.RightHeader = null;
			}
		}
	}

	private void SetSubtitleButton_Click(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			WindowingTitleBar.Subtitle = SubtitleTextBox.Text;
		}
	}

	private void TitleButton_Click(object sender, RoutedEventArgs e)
	{
		if (WindowingTitleBar != null)
		{
			WindowingTitleBar.Title = TitleTextBox.Text;
		}
	}

}
