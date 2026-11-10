using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.ElementNameProbe;

public sealed partial class ENP_xLoadToggle : UserControl
{
	public ENP_xLoadToggle()
	{
		Model = new ENP_xLoadToggle_Model();
		this.InitializeComponent();
	}

	public ENP_xLoadToggle_Model Model { get; }
}

public class ENP_xLoadToggle_Model : INotifyPropertyChanged
{
	private bool _isToggledLoaded = true;

	public event PropertyChangedEventHandler PropertyChanged;

	public bool IsToggledLoaded
	{
		get => _isToggledLoaded;
		set
		{
			_isToggledLoaded = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsToggledLoaded)));
		}
	}
}
