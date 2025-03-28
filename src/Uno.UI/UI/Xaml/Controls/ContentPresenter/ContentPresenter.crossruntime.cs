using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

partial class ContentPresenter
{
	partial void RegisterContentTemplateRoot() => AddChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot() => RemoveChild(ContentTemplateRoot);
}
