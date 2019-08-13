using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ItemsControlTestsControl
{
	[SampleControlInfo("ItemsControl", "ItemContainerStyleSelector", typeof(ListViewViewModel))]
	public sealed partial class ItemsControl_ItemContainerStyleSelector : UserControl
	{
		public ItemsControl_ItemContainerStyleSelector()
		{
			this.InitializeComponent();
		}
	}

	public class EvenOddStyleSelector : StyleSelector
	{
		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			int number;
			if (int.TryParse(item.ToString(), out number))
			{
				var foreground = number % 2 == 0
					? new SolidColorBrush(Colors.Green)  // even
					: new SolidColorBrush(Colors.Red);   // odd

				if (container is Control)
				{
					return new Style(container.GetType())
					{
						Setters =
						{
#if XAMARIN
							new Setter<Control>("Foreground", t => t.Foreground = foreground)
#else
							new Setter(Control.ForegroundProperty, foreground)
#endif
						}
					};
				}
				else if (container is Windows.UI.Xaml.Controls.ContentPresenter)
				{
					return new Style(container.GetType())
					{
						Setters =
						{
#if XAMARIN
							new Setter<Windows.UI.Xaml.Controls.ContentPresenter>("Foreground", t => t.Foreground = foreground)
#else
							new Setter(Windows.UI.Xaml.Controls.ContentPresenter.ForegroundProperty, foreground)
#endif
						}
					};
				}
			}

			return null;
		}
	}
}
