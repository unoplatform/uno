#nullable enable

using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BindableAttribute = Microsoft.UI.Xaml.Data.BindableAttribute;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

public sealed partial class When_VisualState_Setter_Binding : UserControl
{
	public When_VisualState_Setter_Binding()
	{
		this.InitializeComponent();
	}
}

[Bindable]
public sealed class VisualStateSetterBindingViewModel : INotifyPropertyChanged
{
	private string _flaggedText = "flagged";

	public bool IsFlagged { get; set; } = true;

	public string FlaggedText
	{
		get => _flaggedText;
		set
		{
			_flaggedText = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlaggedText)));
		}
	}

	public string OtherText { get; set; } = "other";

	public GridLength ColWidth { get; set; } = new GridLength(123);

	public double Offset { get; set; } = 7;

	public event PropertyChangedEventHandler? PropertyChanged;
}
