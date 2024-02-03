namespace Microsoft.UI.Composition;

public partial class ExpressionAnimation : CompositionAnimation
{
	internal ExpressionAnimation(Compositor compositor) : base(compositor)
	{
	}

	public string Expression { get; set; }
}
