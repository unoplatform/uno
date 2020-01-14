<<<<<<< HEAD:src/SamplesApp/UITests.Shared/Windows_UI_Xaml/TouchEventsTests/Touch.xaml.cs
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using System;
=======
﻿using System;
>>>>>>> Fix Android X dependencies and Api changes:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/Popup/Popup_Overlay_On.xaml.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
<<<<<<< HEAD:src/SamplesApp/UITests.Shared/Windows_UI_Xaml/TouchEventsTests/Touch.xaml.cs

=======
using Uno.UI.Samples.Controls;
>>>>>>> Fix Android X dependencies and Api changes:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/Popup/Popup_Overlay_On.xaml.cs
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

<<<<<<< HEAD:src/SamplesApp/UITests.Shared/Windows_UI_Xaml/TouchEventsTests/Touch.xaml.cs
namespace Uno.UI.Samples.Content.UITests.TouchEventsTests
{
	[SampleControlInfo("Touch", "Touch", typeof(TouchViewModel))]
	public sealed partial class Touch : UserControl
	{
		public Touch()
=======
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.Popup
{
	[SampleControlInfo(description:"Popup with light-dismiss and overlay enabled")]
	public sealed partial class Popup_Overlay_On : UserControl
	{
		public Popup_Overlay_On()
>>>>>>> Fix Android X dependencies and Api changes:src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/Popup/Popup_Overlay_On.xaml.cs
		{
			this.InitializeComponent();
		}
	}
}
