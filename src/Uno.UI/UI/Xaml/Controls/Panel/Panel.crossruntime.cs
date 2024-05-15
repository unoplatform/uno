using System.Collections;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class Panel
{
	partial void OnBackgroundChangedPartial() => UpdateHitTest();
}
