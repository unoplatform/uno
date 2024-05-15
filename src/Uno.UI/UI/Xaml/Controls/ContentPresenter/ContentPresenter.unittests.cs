using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	partial void RegisterContentTemplateRoot() => AddChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot() => RemoveChild(ContentTemplateRoot);
}
