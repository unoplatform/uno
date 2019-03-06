#if !NET461
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Input
{
    public delegate void KeyEventHandler(object sender, KeyRoutedEventArgs e);
}
#endif