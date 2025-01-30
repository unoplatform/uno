#nullable enable

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
