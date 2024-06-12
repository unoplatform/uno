using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ContentPresenterPages;

public class TestControl : ContentControl
{
}

public sealed partial class ContentPresenter_Content : Page
{
	public ContentPresenter_Content()
	{
		this.InitializeComponent();
		OwnedButton.DataContextChanged += OwnedButton_DataContextChanged;
		ContentOwner.DataContext = "Owner DataContext";
		Presenter.DataContext = "Presenter DataContext";
	}

	private void OwnedButton_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		System.Diagnostics.Debug.WriteLine("Button's data context is now " + args.NewValue);
	}

	private void Move_Click(object sender, RoutedEventArgs e)
	{
		Presenter.Content = OwnedButton;
	}

	private void CheckParent_Click(object sender, RoutedEventArgs e)
	{
		if (VisualTreeHelper.GetChildrenCount(Presenter) > 0)
		{
			var child = VisualTreeHelper.GetChild(Presenter, 0);
			System.Diagnostics.Debug.WriteLine($"ContentPresenter contains {(child as FrameworkElement).Name}." +
						 " Its parent is {(child as FrameworkElement)?.Parent}");
		}
	}

	private async void SetNewButton_Click(object sender, RoutedEventArgs e)
	{
		var button = new Button() { Name = "CodeBehindButton", Content = "Code-behind button" };
		Presenter.Content = button;
	}

	private void ChangeDataContext_Click(object sender, RoutedEventArgs e)
	{
		Presenter.DataContext = Guid.NewGuid().ToString();
	}
}
