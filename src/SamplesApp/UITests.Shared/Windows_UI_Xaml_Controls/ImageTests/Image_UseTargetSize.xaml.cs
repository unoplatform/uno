using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
<<<<<<< HEAD:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/ContentControlTestsControl/ContentControl_UnsetContent.xaml.cs
=======
using System.Threading.Tasks;
>>>>>>> Fix Android X dependencies and Api changes:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/ImageTests/Image_UseTargetSize.xaml.cs
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using Windows.Storage;

<<<<<<< HEAD:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/ContentControlTestsControl/ContentControl_UnsetContent.xaml.cs
namespace GenericApp.Views.Content.UITests.ContentControlTestsControl
{
	[SampleControlInfo("ContentControlTestsControl", "ContentControl_UnsetContent", typeof(ContentControlTestViewModel))]
	public sealed partial class ContentControl_UnsetContent : UserControl
	{
		public ContentControl_UnsetContent()
=======
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Samples.UITests.Image
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[SampleControlInfo]
	public sealed partial class Image_UseTargetSize : Page
	{
		public Image_UseTargetSize()
>>>>>>> Fix Android X dependencies and Api changes:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/ImageTests/Image_UseTargetSize.xaml.cs
		{
			this.InitializeComponent();
		}
	}
}
