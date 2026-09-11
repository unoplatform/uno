#nullable enable

using System;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionPathGeometry : CompositionGeometry
	{
		private CompositionPath? _path;

		internal CompositionPathGeometry(Compositor compositor, CompositionPath? path = null) : base(compositor)
		{
			Path = path;
		}

		public CompositionPath? Path
		{
			get => _path;
			set => SetObjectProperty(ref _path, value);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
			=> propertyName.Equals(nameof(Path), StringComparison.OrdinalIgnoreCase)
				? Path!
				: base.GetAnimatableProperty(propertyName, subPropertyName);

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(Path), StringComparison.OrdinalIgnoreCase))
			{
				Path = propertyValue as CompositionPath;
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
