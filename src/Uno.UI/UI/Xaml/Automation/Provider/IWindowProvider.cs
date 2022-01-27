using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Automation.Provider
{
	partial interface IWindowProvider
	{

		global::Windows.UI.Xaml.Automation.WindowInteractionState InteractionState
		{
			get;
		}


		bool IsModal
		{
			get;
		}


		bool IsTopmost
		{
			get;
		}


		bool Maximizable
		{
			get;
		}


		bool Minimizable
		{
			get;
		}


		global::Windows.UI.Xaml.Automation.WindowVisualState VisualState
		{
			get;
		}
		void Close();


		void SetVisualState(global::Windows.UI.Xaml.Automation.WindowVisualState state);


		bool WaitForInputIdle(int milliseconds);

	}
}
