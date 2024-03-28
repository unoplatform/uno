using System.Collections;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

partial class Panel
{
	partial void OnBackgroundChangedPartial() => UpdateHitTest();
}
