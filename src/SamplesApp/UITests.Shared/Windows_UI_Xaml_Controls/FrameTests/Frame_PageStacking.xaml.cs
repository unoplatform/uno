using System;
using System.Collections.Generic;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.FrameTests;

[Sample("Frame", IgnoreInSnapshotTests = true)]
public sealed partial class Frame_PageStacking : Page
{
	public Frame_PageStacking()
	{
		this.InitializeComponent();
	}

	private void NavigateBack(object sender, RoutedEventArgs e)
		=> MyFrame.GoBack(new SuppressNavigationTransitionInfo());

	private void NavigateRed(object sender, RoutedEventArgs e)
		=> MyFrame.Navigate(typeof(FrameTests_RedPage), default, new SuppressNavigationTransitionInfo());

	private void NavigateTransparent(object sender, RoutedEventArgs e)
		=> MyFrame.Navigate(typeof(FrameTests_TransparentPage), default, new SuppressNavigationTransitionInfo());

	/// <inheritdoc />
	protected override void OnTapped(TappedRoutedEventArgs e)
		=> TappedResult.Text = "tapped";

	private void HandleTappedOnNavBar(object sender, TappedRoutedEventArgs e)
		=> e.Handled = true;
}
