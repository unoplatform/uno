using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Globalization;

namespace Uno.UI.Samples.Content.UITests.DeferLoadStrategy
{
	[Sample("XAML", Name = "DeferLoadStrategyWithTemplateBinding", ViewModelType = typeof(Presentation.SamplePages.DeferLoadStrategyViewModel),
		Description = "DeferLoadStrategyWithTemplateBinding - content should load after a brief delay")]
	public sealed partial class DeferLoadStrategyWithTemplateBinding : UserControl
	{
		public DeferLoadStrategyWithTemplateBinding()
		{
			this.InitializeComponent();
		}
	}
}
