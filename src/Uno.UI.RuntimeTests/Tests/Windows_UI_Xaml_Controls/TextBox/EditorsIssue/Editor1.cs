#nullable enable

using System.ComponentModel;

namespace MobileTemplateSelectorIssue.Editors
{
	public sealed class Editor1 : IEditor
	{
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
				}
			}
		}
		private string _text = "Editor1";

		public event PropertyChangedEventHandler? PropertyChanged = null;
	}
}
