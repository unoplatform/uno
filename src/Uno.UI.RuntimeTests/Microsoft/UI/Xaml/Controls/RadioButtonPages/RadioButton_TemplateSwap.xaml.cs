using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.RadioButtonPages;

public sealed partial class RadioButton_TemplateSwap : UserControl
{
	public RadioButton_TemplateSwap()
	{
		InitializeComponent();
	}

	public PickerTemplateSelector Selector => (PickerTemplateSelector)Resources["PickerTemplateSelector"];
}

public enum PickerKind
{
	Radio,
	CheckBox,
}

public class PickerModel(PickerKind kind, string name, params string[] optionLabels)
{
	public PickerKind Kind { get; } = kind;

	public string Name { get; } = name;

	public ObservableCollection<PickerOption> Options { get; } = [.. optionLabels.Select(l => new PickerOption(l))];

	public string SelectedLabels => string.Join(",", Options.Where(o => o.IsSelected).Select(o => o.Label));

	public override string ToString() => $"{Name}: [{SelectedLabels}]";
}

public class PickerOption(string label) : INotifyPropertyChanged
{
	private bool _isSelected;

	public event PropertyChangedEventHandler PropertyChanged;

	public string Label { get; } = label;

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (_isSelected == value)
			{
				return;
			}

			_isSelected = value;
			PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
		}
	}

	public override string ToString() => $"{Label}={_isSelected}";
}

public partial class PickerTemplateSelector : DataTemplateSelector
{
	public DataTemplate RadioTemplate { get; set; }

	public DataTemplate CheckBoxTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item) => SelectTemplateCore(item, null);

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => item switch
	{
		PickerModel { Kind: PickerKind.Radio } => RadioTemplate,
		PickerModel { Kind: PickerKind.CheckBox } => CheckBoxTemplate,
		_ => null,
	};
}
