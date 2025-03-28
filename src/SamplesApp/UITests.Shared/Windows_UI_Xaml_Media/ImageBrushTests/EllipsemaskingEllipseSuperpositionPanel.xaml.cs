using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using UITests.Shared.Helpers;
using System.Threading.Tasks;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[SampleControlInfo("Brushes", "EllipsemaskingEllipseSuperpositionPanel")]
	public sealed partial class EllipsemaskingEllipseSuperpositionPanel : UserControl, IWaitableSample
	{
		private readonly Task _samplePreparedTask;

		public EllipsemaskingEllipseSuperpositionPanel()
		{
			this.InitializeComponent();
			_samplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(imageBrush1, imageBrush2);
		}

		public Task SamplePreparedTask => _samplePreparedTask;
	}
}
