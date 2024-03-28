using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls;

partial class ContentPresenter
{
	partial void RegisterContentTemplateRoot() => AddChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot() => RemoveChild(ContentTemplateRoot);
}
