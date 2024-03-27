using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Core;

partial class ContentRoot
{
	internal enum ChangeType
	{
		Size,
		RasterizationScale,
		IsVisible,
		Content
	};

	internal void AddPendingXamlRootChangedEvent(ContentRoot.ChangeType _/*ignored for now*/) => _hasPendingChangedEvent = true;

	private bool _hasPendingChangedEvent;
}
