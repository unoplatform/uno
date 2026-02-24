using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;

namespace Microsoft.UI.Xaml;

public delegate void WindowSizeChangedEventHandler(object sender, WindowSizeChangedEventArgs e);
