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

namespace UITests.Shared.Windows_UI_Xaml.Memory
{
	[SampleControlInfo("Memory", "GarbageCollection")]
	public sealed partial class GarbageCollection : UserControl
	{
		public GarbageCollection()
		{
			this.InitializeComponent();
		}

		private void Add(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < 100; i++)
			{
				var child = new MyContentControl
				{
					Content = new Button
					{
						Content = new MyContentControl
						{
							Content = new TextBlock { Text = "Text" }
						}
					}
				};
				Container.Children.Add(child);
			}
		}

		private void Clear(object sender, RoutedEventArgs e)
		{
			Container.Children.Clear();
		}

		private void Refresh(object sender, RoutedEventArgs e)
		{
			GC.Collect();
			Info.Text = $"Created instances: {MyContentControl.InstanceCount}\nActive instances: {MyContentControl.ActiveInstanceCount}";
		}
	}

	public partial class MyContentControl : ContentControl
	{
		public static List<WeakReference> Instances = new List<WeakReference>();

		public static int ActiveInstanceCount => Instances.Count(i => i.IsAlive);
		public static int InstanceCount => Instances.Count();

		public MyContentControl()
		{
			Instances.Add(new WeakReference(this));
		}
	}
}

