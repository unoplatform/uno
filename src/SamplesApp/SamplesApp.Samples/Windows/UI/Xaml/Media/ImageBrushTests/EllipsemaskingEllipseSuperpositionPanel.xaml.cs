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
using UITests.Shared.Helpers;
using System.Threading.Tasks;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[Sample("Brushes", Name = "EllipsemaskingEllipseSuperpositionPanel")]
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
