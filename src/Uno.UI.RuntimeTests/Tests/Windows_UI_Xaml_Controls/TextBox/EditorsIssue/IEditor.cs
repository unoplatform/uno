using System.ComponentModel;

namespace MobileTemplateSelectorIssue.Editors
{
	public interface IEditor : INotifyPropertyChanged
	{
		string Text { get; set; }
	}
}
