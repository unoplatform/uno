using System;
using System.Collections.Generic;
using Uno.Disposables;
using Windows.UI;

namespace Windows.UI.Composition
{
	public partial class CompositionColorBrush : CompositionBrush
	{
		private List<Action> _colorChangedHandlers = new List<Action>();
		private Color _color;

		internal CompositionColorBrush() => throw new NotSupportedException();

		public CompositionColorBrush(Compositor compositor) : base(compositor)
		{

		}

		public Color Color
		{
			get { return _color; }
			set { _color = value; OnColorChanged(); }
		}

		private void OnColorChanged()
		{
			foreach (var handler in _colorChangedHandlers)
			{
				handler();
			}
		}

		internal IDisposable RegisterColorChanged(Action onColorChanged)
		{
			_colorChangedHandlers.Add(onColorChanged);

			return Disposable.Create(() => _colorChangedHandlers.Remove(onColorChanged));
		}
	}
}
