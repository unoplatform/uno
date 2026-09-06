using Uno.Extensions;

namespace Windows.UI.Text
{
	partial struct FontWeight
	{
		internal string ToCssString()
		{
			return Weight == 400 ? "normal" : Weight.ToStringInvariant();
		}
	}
}
