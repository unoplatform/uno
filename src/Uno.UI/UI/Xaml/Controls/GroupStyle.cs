#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GroupStyle
	{
		private DataTemplate? _headerTemplate;
		private DataTemplateSelector? _headerTemplateSelector;
		private Style? _headerContainerStyle;
		private bool _hidesIfEmpty;

		public DataTemplate? HeaderTemplate
		{
			get => _headerTemplate;
			set => Set(ref _headerTemplate, value);
		}

		public DataTemplateSelector? HeaderTemplateSelector
		{
			get => _headerTemplateSelector;
			set => Set(ref _headerTemplateSelector, value);
		}

		public Style? HeaderContainerStyle
		{
			get => _headerContainerStyle;
			set => Set(ref _headerContainerStyle, value);
		}

		public bool HidesIfEmpty
		{
			get => _hidesIfEmpty;
			set => Set(ref _hidesIfEmpty, value);
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (!Equals(field, value))
			{
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
