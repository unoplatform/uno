using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public sealed partial class When_TwoWay_Text_Binding : Page
{
	public class VM : INotifyPropertyChanged
	{
		private string _vmProperty;

		public event PropertyChangedEventHandler PropertyChanged;

		public string VMProperty
		{
			get => _vmProperty;
			set
			{
				_vmProperty = value;
				SetCount++;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VMProperty)));
			}
		}

		public int SetCount { get; private set; }
	}

	public VM VMForXBind { get; } = new VM();

	public When_TwoWay_Text_Binding()
	{
		this.InitializeComponent();
		tbTwoWay_triggerDefault.DataContext = new VM();
		tbTwoWay_triggerLostFocus.DataContext = new VM();
		tbTwoWay_triggerPropertyChanged.DataContext = new VM();
		tbTwoWay_triggerExplicit.DataContext = new VM();
	}
}
