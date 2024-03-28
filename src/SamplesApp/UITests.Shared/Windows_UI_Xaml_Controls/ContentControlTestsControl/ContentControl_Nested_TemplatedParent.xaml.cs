using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Helper;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using View = Windows.UI.Xaml.UIElement;

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	[SampleControlInfo(
		"ContentControl",
		"ContentControl_Nested_TemplatedParent",
		description: "This test validates the TemplatedParent propagation with loaded/unloaded content cycles"
	)]
	public sealed partial class ContentControl_Nested_TemplatedParent : UserControl
	{
		public ContentControl_Nested_TemplatedParent()
		{
			this.InitializeComponent();

			this.RunWhileLoaded(Run);
		}

		private async Task Run(CancellationToken ct)
		{
			MyContent = new MyNestedContent() { MyProperty = 42 };
			View content = null;

			while (!ct.IsCancellationRequested)
			{
				if (rootGrid.Children.Any())
				{
					content = rootGrid.Children.First() as UIElement;
					rootGrid.Children.Remove(content);
				}
				else
				{
					rootGrid.Children.Add(content);
				}

				await Task.Delay(1000);
			}

			if (rootGrid.Children.Count == 0 && content != null)
			{
				rootGrid.Children.Add(content);
			}
		}

		public MyNestedContent MyContent
		{
			get { return (MyNestedContent)GetValue(MyContentProperty); }
			set { SetValue(MyContentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyContent.  This enables animation, styling, binding, etc...
		public static DependencyProperty MyContentProperty { get; } =
			DependencyProperty.Register("MyContent", typeof(MyNestedContent), typeof(ContentControl_Nested_TemplatedParent), new PropertyMetadata(null));
	}

	public partial class MyNestedContent : DependencyObject
	{
		public int MyProperty
		{
			get { return (int)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static DependencyProperty MyPropertyProperty { get; } =
			DependencyProperty.Register("MyProperty", typeof(int), typeof(MyNestedContent), new PropertyMetadata(0));
	}
}
