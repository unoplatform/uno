using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace XamlGenerationTests.Shared.NamespaceClashPoint
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class EnumValueNamespaceTest : Page
	{
		public EnumValueNamespaceTest()
		{
			this.InitializeComponent();
		}
	}
}

namespace NamespaceClashPoint.Example
{
	public enum Asd { A, S, D }

	public static class GridAsdHelper
	{
		#region Property: Asd
		public static readonly DependencyProperty AsdProperty = DependencyProperty.RegisterAttached(
			"Asd",
			typeof(Asd),
			typeof(GridAsdHelper),
			new FrameworkPropertyMetadata(default(Asd)));

		public static Asd GetAsd(Grid obj) => (Asd)obj.GetValue(AsdProperty);
		public static void SetAsd(Grid obj, Asd value) => obj.SetValue(AsdProperty, value);
		#endregion
	}
}
