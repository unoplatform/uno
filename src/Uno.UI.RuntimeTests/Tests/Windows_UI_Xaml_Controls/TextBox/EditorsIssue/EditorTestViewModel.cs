#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MobileTemplateSelectorIssue.Editors;

namespace MobileTemplateSelectorIssue
{
	public sealed class EditorTestViewModel : INotifyPropertyChanged
	{
		public IEnumerable<IEditor> Editors { get; private set; }

		public IEditor CurrentEditor
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
		IEditor _currentEditor;

		public IEditor FirstEditor { get; }

		public EditorTestViewModel()
		{
			Editors = new IEditor[] { new Editor1(), new Editor2() };
			_currentEditor = Editors.First();
			FirstEditor = _currentEditor;
		}

		public event PropertyChangedEventHandler? PropertyChanged = null;
	}
}
