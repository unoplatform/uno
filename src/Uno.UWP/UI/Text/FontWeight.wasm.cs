#nullable disable

using Uno.Extensions;

namespace Windows.UI.Text
{
	partial struct FontWeight
	{
		public string ToCssString()
		{
			return Weight == 400 ? "normal" : Weight.ToStringInvariant();
		}
	}
}
