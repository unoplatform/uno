using System.Windows.Input;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.Models
{
	public class ChatBoxViewModel : ViewModelBase
	{
		public ChatBoxViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{			
			ToggleHeader = CreateCommand(ExecuteToggleHeader);
			m_ClickCount = 0;
		}

		public ICommand ToggleHeader { get; }

		private int m_ClickCount;

		public int ClickCount {
			get { return m_ClickCount; }
			set
			{
				m_ClickCount = value;
				RaisePropertyChanged();
			}
		}

		private void ExecuteToggleHeader()
		{
			ClickCount ++;
		}
	}
}
