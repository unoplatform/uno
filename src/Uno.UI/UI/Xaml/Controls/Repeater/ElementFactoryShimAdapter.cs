#nullable enable

namespace Microsoft.UI.Xaml.Controls;

// Lets ItemsRepeater, which calls its ItemTemplate through IElementFactoryShim, use any Microsoft.UI.Xaml.IElementFactory as WinUI does.
internal sealed class ElementFactoryShimAdapter : IElementFactoryShim
{
	private readonly global::Microsoft.UI.Xaml.ElementFactoryGetArgs _getArgs = new();
	private readonly global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs _recycleArgs = new();

	public ElementFactoryShimAdapter(IElementFactory factory) => Factory = factory;

	internal IElementFactory Factory { get; }

	public UIElement GetElement(ElementFactoryGetArgs args)
	{
		_getArgs.Data = args.Data;
		_getArgs.Parent = args.Parent;
		try
		{
			return Factory.GetElement(_getArgs);
		}
		finally
		{
			_getArgs.Data = null;
			_getArgs.Parent = null;
		}
	}

	public void RecycleElement(ElementFactoryRecycleArgs args)
	{
		_recycleArgs.Element = args.Element;
		_recycleArgs.Parent = args.Parent;
		try
		{
			Factory.RecycleElement(_recycleArgs);
		}
		finally
		{
			_recycleArgs.Element = null;
			_recycleArgs.Parent = null;
		}
	}
}
