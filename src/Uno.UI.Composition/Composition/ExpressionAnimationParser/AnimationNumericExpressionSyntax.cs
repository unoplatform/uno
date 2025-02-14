namespace Windows.UI.Composition;

internal class AnimationNumericExpressionSyntax : AnimationExpressionSyntax
{
	private readonly ExpressionAnimationToken _number;

	public AnimationNumericExpressionSyntax(ExpressionAnimationToken number)
	{
		_number = number;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		return _number.Value;
	}
}
