#nullable enable

using System;
using System.ComponentModel;

namespace FrameworkPoolEditorRecycling.Editors
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
			get => _text;
			set
			{
				if (_text != value)
				{
					_text = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
				}
			}
		}

		public bool IsChecked
		{
			get => _isChecked;
			set
			{
				if (_isChecked != value)
				{
					_isChecked = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
				}
			}
		}

		public bool IsOn
		{
			get => _isOn;
			set
			{
				if (_isOn != value)
				{
					_isOn = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOn)));
				}
			}
		}

		public Type EditorViewType { get; }

		private string _text = "Editor";
		private bool _isChecked = true;
		private bool _isOn = true;

		public event PropertyChangedEventHandler? PropertyChanged = null;
	}
}
