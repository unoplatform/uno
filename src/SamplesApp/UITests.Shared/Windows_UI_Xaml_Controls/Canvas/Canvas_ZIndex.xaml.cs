using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
<<<<<<< HEAD:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/ContentControlTestsControl/ContentControl_FindName.xaml.cs
using Uno.UI.Samples.Presentation.SamplePages;

=======
>>>>>>> Fix Android X dependencies and Api changes:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/Canvas/Canvas_ZIndex.xaml.cs
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

<<<<<<< HEAD:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/ContentControlTestsControl/ContentControl_FindName.xaml.cs
namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	[SampleControlInfo("ContentControlTestsControl", "ContentControl_FindName")]
	public sealed partial class ContentControl_FindName : UserControl
	{
		public ContentControl_FindName()
=======
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.Canvas
{
	[SampleControlInfo(description: "Demonstrates correct application of Canvas.ZIndex")]
	public sealed partial class Canvas_ZIndex : UserControl
	{
		public Canvas_ZIndex()
>>>>>>> Fix Android X dependencies and Api changes:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/Canvas/Canvas_ZIndex.xaml.cs
		{
			this.InitializeComponent();
		}
	}
}
