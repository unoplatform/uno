using System.Web;
using Uno.UI.UI.Xaml.Documents;

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
