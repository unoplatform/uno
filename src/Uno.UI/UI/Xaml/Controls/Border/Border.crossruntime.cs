using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using System.Linq;
using System.Drawing;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Uno.UI;

using View = Windows.UI.Xaml.UIElement;
using Color = System.Drawing.Color;
using Windows.UI.Composition;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

partial class Border
{
	partial void OnBackgroundChangedPartial() => UpdateHitTest();
}
