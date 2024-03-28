using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System.Drawing;
using View = Windows.UI.Xaml.UIElement;


namespace Windows.UI.Xaml.Controls;

partial class Panel
{
	public override IEnumerable<View> GetChildren() => Children.OfType<View>().ToArray<View>();
}
