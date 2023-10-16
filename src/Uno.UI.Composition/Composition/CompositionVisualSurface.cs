#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Composition
{
	public partial class CompositionVisualSurface : CompositionObject, ICompositionSurface
	{
		private Visual? _sourceVisual;
		private Vector2 _sourceOffset;
		private Vector2 _sourceSize;

		public CompositionVisualSurface(Compositor compositor) : base(compositor)
		{

		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			// Call base implementation - Visual calls Compositor.InvalidateRender().
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(SourceVisual):
					OnSourceVisualChangedPartial(SourceVisual);
					break;
				case nameof(SourceOffset):
					OnSourceOffsetChangedPartial(SourceOffset);
					break;
				case nameof(SourceSize):
					OnSourceSizeChangedPartial(SourceSize);
					break;
				default:
					break;
			}
		}

		partial void OnSourceVisualChangedPartial(Visual? sourceVisual);
		partial void OnSourceOffsetChangedPartial(Vector2 offset);
		partial void OnSourceSizeChangedPartial(Vector2 size);

		public Visual? SourceVisual
		{
			get => _sourceVisual;
			set => SetProperty(ref _sourceVisual, value);
		}

		public Vector2 SourceOffset
		{
			get => _sourceOffset;
			set => SetProperty(ref _sourceOffset, value);
		}

		public Vector2 SourceSize
		{
			get => _sourceSize;
			set => SetProperty(ref _sourceSize, value);
		}
	}
}
