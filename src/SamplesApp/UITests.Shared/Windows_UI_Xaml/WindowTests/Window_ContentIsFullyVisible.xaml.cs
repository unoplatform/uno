using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Uno.UI.Samples.Controls;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml.WindowTests
{
	[Sample("Windowing", Description = "There should be a fully visible orange border.")]
	public sealed partial class Window_ContentIsFullyVisible : UserControl
	{
		public Window_ContentIsFullyVisible()
		{
			this.InitializeComponent();
			Window_ContentIsFullyVisible_Content.SizeChanged += Window_ContentIsFullyVisible_Content_SizeChanged;
			SetDiagnosticInfoTextBlocksText();
		}

		private void Window_ContentIsFullyVisible_Content_SizeChanged(object sender, SizeChangedEventArgs args) => SetDiagnosticInfoTextBlocksText();

		private void SetDiagnosticInfoTextBlocksText()
		{
			// Diagnostic information that's helpful if this test fails and you want to examine a screenshot to compare the reported size of the content versus its actual size. Remember to uncomment the TextBlocks in the Xaml file.

			//DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
			//ScaledActualWidthTextBlock.Text = $"Scaled {nameof(Window_ContentIsFullyVisible_Content.ActualWidth)} = {Window_ContentIsFullyVisible_Content.ActualWidth * displayInformation.RawPixelsPerViewPixel}";
			//ScaledActualHeightTextBlock.Text = $"Scaled {nameof(Window_ContentIsFullyVisible_Content.ActualHeight)} = {Window_ContentIsFullyVisible_Content.ActualHeight * displayInformation.RawPixelsPerViewPixel}";
			//LogicalDpiTextBlock.Text = $"{nameof(DisplayInformation.LogicalDpi)} = {displayInformation.LogicalDpi}";
			//RawPixelsPerViewPixelTextBlock.Text = $"{nameof(DisplayInformation.RawPixelsPerViewPixel)} = {displayInformation.RawPixelsPerViewPixel}";
			//ResolutionScaleTextBlock.Text = $"{nameof(DisplayInformation.ResolutionScale)} = {displayInformation.ResolutionScale}";
			//ActualWidthTextBlock.Text = $"{nameof(Window_ContentIsFullyVisible_Content.ActualWidth)} = {Window_ContentIsFullyVisible_Content.ActualWidth}";
			//ActualHeightTextBlock.Text = $"{nameof(Window_ContentIsFullyVisible_Content.ActualHeight)} = {Window_ContentIsFullyVisible_Content.ActualHeight}";
		}
	}
}
