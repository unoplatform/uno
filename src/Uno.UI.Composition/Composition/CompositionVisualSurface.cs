#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.Disposables;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionVisualSurface : CompositionObject, ICompositionSurface, ISkiaSurface
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

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(SourceOffset), StringComparison.OrdinalIgnoreCase))
			{
				return SubPropertyHelpers.GetVector2(subPropertyName, SourceOffset);
			}
			else if (propertyName.Equals(nameof(SourceSize), StringComparison.OrdinalIgnoreCase))
			{
				return SubPropertyHelpers.GetVector2(subPropertyName, SourceSize);
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(SourceOffset), StringComparison.OrdinalIgnoreCase))
			{
				SourceOffset = SubPropertyHelpers.ValidateValue<Vector2>(propertyValue);
			}
			else if (propertyName.Equals(nameof(SourceSize), StringComparison.OrdinalIgnoreCase))
			{
				SourceSize = SubPropertyHelpers.ValidateValue<Vector2>(propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}

		void ISkiaSurface.Paint(SKCanvas canvas, float opacity)
		{
			if (SourceVisual is not null)
			{
				int save = canvas.Save();
				// Note that this is applied before the SourceOffset translates the canvas' matrix, so
				canvas.ClipRect(new SKRect(0, 0, (this as ISkiaSurface).Size.X, (this as ISkiaSurface).Size.X), antialias: true);

				SourceVisual.RenderRootVisual(canvas, SourceOffset);
				canvas.RestoreToCount(save);
			}
		}

		Vector2 ISkiaSurface.Size => SourceSize switch
		{
			{ X: > 0.0f, Y: > 0.0f } => SourceSize,
			_ => SourceVisual switch
			{
				{ Size: { X: > 0.0f, Y: > 0.0f } } => SourceVisual.Size,
				_ => new Vector2(1000, 1000)
			}
		};
	}
}
