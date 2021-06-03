using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Tests.App.Views;
using Uno.UI.Tests.ViewLibrary;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.App.Xaml
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Subclassed_Dictionary_With_Property : ResourceDictionary
	{
		public string Test
		{
			get => (string)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(
				nameof(Test),
				typeof(string),
				typeof(Subclassed_Dictionary_With_Property),
				new PropertyMetadata(OnPropertyChanged));

		private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is Subclassed_Dictionary_With_Property rd)
			{
				if (!rd.ContainsKey("TestKey"))
				{
					rd.Add("TestKey", args.NewValue as string);
				}
				else
				{
					rd["TestKey"] = args.NewValue as string;
				}
			}
		}

		public Subclassed_Dictionary_With_Property()
		{
			this.InitializeComponent();
		}
	}

}
