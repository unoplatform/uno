using System.Web;

namespace Windows.UI.Xaml.Documents
{
	partial class Run
	{
		partial void OnTextChangedPartial()
		{
			this.SetText(Text);
		}
	}
}
