namespace Windows.UI.Composition;

public partial class CompositionTarget
{
	private Visual _root;
	internal CompositionTarget()
	{
	}

	public Visual Root
	{
		get => _root;
		set
		{
			_root = value;
			_root.CompositionTarget = this;
		}
	}
}
