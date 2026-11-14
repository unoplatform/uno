#nullable enable

namespace Microsoft.UI.Xaml.Controls;

// Lets an IElementFactoryShim that isn't also a Microsoft.UI.Xaml.IElementFactory be used as an ItemTemplate,
// since the controls only consume IElementFactory, as in WinUI. ElementFactory implements both, so it doesn't need this.
// TODO Uno: remove with IElementFactoryShim.
internal sealed class ElementFactoryShimAdapter : global::Microsoft.UI.Xaml.IElementFactory
{
	private readonly IElementFactoryShim _shim;

	// Spare args, taken while in use so a re-entrant call allocates its own instead of clearing the caller's.
	private ElementFactoryGetArgs? _getArgs;
	private ElementFactoryRecycleArgs? _recycleArgs;

	public ElementFactoryShimAdapter(IElementFactoryShim shim) => _shim = shim;

	public UIElement GetElement(global::Microsoft.UI.Xaml.ElementFactoryGetArgs args)
	{
		var shimArgs = _getArgs ?? new ElementFactoryGetArgs();
		_getArgs = null;
		try
		{
			shimArgs.Data = args.Data;
			shimArgs.Parent = args.Parent;
			shimArgs.Index = args.Index;
			return _shim.GetElement(shimArgs);
		}
		finally
		{
			shimArgs.Data = null;
			shimArgs.Parent = null;
			_getArgs = shimArgs;
		}
	}

	public void RecycleElement(global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
	{
		var shimArgs = _recycleArgs ?? new ElementFactoryRecycleArgs();
		_recycleArgs = null;
		try
		{
			shimArgs.Element = args.Element;
			shimArgs.Parent = args.Parent;
			_shim.RecycleElement(shimArgs);
		}
		finally
		{
			shimArgs.Element = null;
			shimArgs.Parent = null;
			_recycleArgs = shimArgs;
		}
	}
}
