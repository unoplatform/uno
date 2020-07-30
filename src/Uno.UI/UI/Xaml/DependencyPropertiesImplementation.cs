
using System;
using System.Linq;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#if XAMARIN_IOS
using Color = UIKit.UIColor;
using View = UIKit.UIView;
#elif __MACOS__
using Color = AppKit.NSColor;
using View = AppKit.NSView;
#elif XAMARIN_ANDROID
using Color = Android.Resource.Color;
using View = Android.Views.View;
#elif NET461 || NETSTANDARD2_0
using Color = System.Object;
using View = Windows.UI.Xaml.FrameworkElement;
#endif  


