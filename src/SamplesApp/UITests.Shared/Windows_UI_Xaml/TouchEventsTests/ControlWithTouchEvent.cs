using Uno.Extensions;
using Uno.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.Text;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Samples.Controls
{
    public partial class ControlWithTouchEvent : Control
	{
#pragma warning disable CS0109
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#pragma warning restore CS0109

		public ControlWithTouchEvent()
        {
            this.Tapped += OnTapped;
            this.PointerPressed += OnPointerPressed;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
			_log.Warn("Event: PointerPressed");
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
			_log.Warn("Event: Tapped.");
        }
        
    }
}
