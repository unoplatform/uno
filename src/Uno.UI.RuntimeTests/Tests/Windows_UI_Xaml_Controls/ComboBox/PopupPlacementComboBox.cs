#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.ComboBoxTests;

public partial class PopupPlacementComboBox : ComboBox
{
	public PopupPlacementComboBox() : base()
	{
		this.Tapped += MyComboBox_Tapped;
	}

	public double VerticalOffset { get; set; }

	public PopupPlacementMode DesiredPlacement { get; set; }

	public void ApplyPlacement()
	{
		if (FindChildrenOfType<Popup>(this).FirstOrDefault() is Popup popup)
		{
			popup.PlacementTarget = this;
			popup.DesiredPlacement = DesiredPlacement;
			popup.VerticalOffset = VerticalOffset;
		}
	}

	private void MyComboBox_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		ApplyPlacement();
	}

	public static IEnumerable<T> FindChildrenOfType<T>(DependencyObject parent) where T : DependencyObject
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);

			if (child is T hit)
			{
				yield return hit;
			}

			foreach (T? grandChild in FindChildrenOfType<T>(child))
			{
				yield return grandChild;
			}
		}
	}
}
