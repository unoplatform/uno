#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FrameworkPoolEditorRecycling.Editors;

namespace FrameworkPoolEditorRecycling
{
	public sealed class EditorTestViewModel : INotifyPropertyChanged
	{
		public EditorViewModel[] Editors { get; private set; }

		public EditorViewModel CurrentEditor
		{
			get => _currentEditor;
			set
			{
				if (_currentEditor != value)
				{
					_currentEditor = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEditor)));
				}
			}
		}

		public void SetNextEditor()
		{
			CurrentEditor = Editors[(Array.IndexOf(Editors, CurrentEditor) + 1) % Editors.Length];
		}

		EditorViewModel _currentEditor;

		public EditorViewModel FirstEditor { get; }

		public EditorTestViewModel()
		{
			Editors = new EditorViewModel[]
			{
				new (typeof(EditorBindingView)),
				new (typeof(EditorBindingPropertyChangedView)),
				new (typeof(EditorXBindView)),
				new (typeof(EditorXBindPropertyChangedView)),
				new (typeof(EditorBindingView)),
				new (typeof(EditorBindingPropertyChangedView)),
				new (typeof(EditorXBindView)),
				new (typeof(EditorXBindPropertyChangedView))
			};
			_currentEditor = Editors.First();
			FirstEditor = _currentEditor;
		}

		public event PropertyChangedEventHandler? PropertyChanged = null;
	}
}
