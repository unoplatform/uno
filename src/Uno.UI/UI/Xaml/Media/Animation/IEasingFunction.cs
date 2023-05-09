namespace Windows.UI.Xaml.Media.Animation
{
	public interface IEasingFunction
	{
		/// <summary>
		/// This method is used to transform frames of an animation to have a certain effect. 
		/// </summary>
		/// <param name="currentTime">Can be seconds or frame number</param>
		/// <param name="startValue"></param>
		/// <param name="finalValue"></param>
		/// <param name="duration">Can be seconds or frame number</param>
		/// <returns>Transformed double value at current time for the selected ease function</returns>
		double Ease(double currentTime, double startValue, double finalValue, double duration);
	}
}
