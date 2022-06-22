using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	using VmType = Controls_Binding.Binding_SameClassName;

	public sealed partial class Binding_SameClassName : UserControl
	{
		public Binding_SameClassName()
		{
			this.InitializeComponent();
		}

		public static readonly DependencyProperty VMProperty =
		   DependencyProperty.Register(
			   nameof(VM),
			   typeof(VmType),
			   typeof(Binding_SameClassName),
			   null);

		public VmType VM
		{
			get => (VmType)GetValue(VMProperty);
			set => SetValue(VMProperty, value);
		}
	}
}


namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls_Binding
{
	public class Binding_SameClassName
	{
		public void OnClick()
		{
		}
	}
}
