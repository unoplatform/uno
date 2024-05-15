using Android.Text;

namespace Uno.UI.Extensions
{
	public static class InputTypesExtensions
	{
		public static bool HasPasswordFlag(this InputTypes inputTypes)
		{
			if (inputTypes.HasFlag(InputTypes.NumberVariationPassword)
				|| inputTypes.HasFlag(InputTypes.TextVariationPassword)
				|| inputTypes.HasFlag(InputTypes.TextVariationVisiblePassword)
				|| inputTypes.HasFlag(InputTypes.TextVariationWebPassword))
			{
				return true;
			}

			return false;
		}
	}
}
