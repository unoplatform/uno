using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Logging;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Samples.Controls
{
    public partial class ControlWithTouchEvent : Control
    {
        public ControlWithTouchEvent()
        {
            this.Tapped += OnTapped;
            this.PointerPressed += OnPointerPressed;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.Log().Warn("Event: PointerPressed");
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            this.Log().Warn("Event: Tapped.");
        }
        
    }
}
