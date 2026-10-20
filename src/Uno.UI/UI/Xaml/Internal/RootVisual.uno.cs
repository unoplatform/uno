#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Core;

partial class RootVisual
{
	UnoRootElementLogic IRootElement.RootElementLogic => _rootElementLogic;

	protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
	{
		base.OnBringIntoViewRequested(args);

		_rootElementLogic.OnBringIntoViewRequested(args);
	}
}
