using System.Web;

namespace Microsoft.UI.Xaml.Documents
{
	partial class Run
	{
		partial void OnTextChangedPartial()
		{
			this.SetText(Text);
		}
	}
}
