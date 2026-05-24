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

namespace UITests.Xaml_CodeGen
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class BindableMetadataGlobalizationTest : Page
	{
		// #2815: generic type parameter are not prefixed with `global::` during BindableTypeProvidersSourceGenerator
		// which causes CS0234, "type or namespace name X not found in Y", in some case like here.
		// the BindableMetadata are generated with a RootNamespace `SamplesApp.${platform}`, and
		// `UITests.Xaml_CodeGen.TestAsd` is then resolved to `SamplesApp.UITests.Xaml_CodeGen.TestAsd`
		// since `SamplesApp.UITests` has a higher precedence than `global::UITests`, even if `Xaml_CodeGen` is not declared there.
		public IEnumerable<TestAsd> MyProperty { get; set; }

		public BindableMetadataGlobalizationTest()
		{
			this.InitializeComponent();
		}
	}

	public class TestAsd { }
}
