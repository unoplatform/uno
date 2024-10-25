#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Composition
{
	public partial class CompositionMaskBrush : CompositionBrush
	{
		private CompositionBrush? _source;
		private CompositionBrush? _mask;

		internal CompositionMaskBrush(Compositor compositor) : base(compositor)
		{

		}

		public CompositionBrush? Source
		{
			get => _source;
			set => SetProperty(ref _source, value);
		}

		public CompositionBrush? Mask
		{
			get => _mask;
			set => SetProperty(ref _mask, value);
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			// Call base implementation - Visual calls Compositor.InvalidateRender().
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Source):
					OnSourceChangedPartial(Source);
					break;
				case nameof(Mask):
					OnMaskChangedPartial(Mask);
					break;
				default:
					break;
			}
		}

		partial void OnSourceChangedPartial(CompositionBrush? source);
		partial void OnMaskChangedPartial(CompositionBrush? mask);
	}
}
