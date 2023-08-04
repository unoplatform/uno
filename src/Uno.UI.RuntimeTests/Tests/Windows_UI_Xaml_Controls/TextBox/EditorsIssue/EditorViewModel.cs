#nullable enable

using System;
using System.ComponentModel;

namespace TextBoxEditorRecycling.Editors
{
	public sealed class EditorViewModel
	{
		public EditorViewModel(Type editorViewType)
		{
			EditorViewType = editorViewType;
			_text = editorViewType.Name;
		}

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

		public Type EditorViewType { get; }

		private string _text = "Editor";

		public event PropertyChangedEventHandler? PropertyChanged = null;
	}
}
