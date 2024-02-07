using Microsoft.UI.Composition;
namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private Visual _root;

	internal Visual Root
	{
		get => _root;
		set
		{
			_root = value;
			_root.CompositionTarget = this;
		}
	}
}
