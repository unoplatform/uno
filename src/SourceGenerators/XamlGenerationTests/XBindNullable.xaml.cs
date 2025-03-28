#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace XamlGenerationTests
{
	public sealed partial class XBindNullable : Page
	{
		public XBindNullable()
		{
			this.InitializeComponent();
		}

		public string? TestProperty { get; set; } = null;
		public bool? TestProperty2 { get; set; } = null;
	}
}
