using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Automation.Provider
{
	partial interface IWindowProvider
	{

		global::Microsoft.UI.Xaml.Automation.WindowInteractionState InteractionState
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


		global::Microsoft.UI.Xaml.Automation.WindowVisualState VisualState
		{
			get;
		}
		void Close();


		void SetVisualState(global::Microsoft.UI.Xaml.Automation.WindowVisualState state);


		bool WaitForInputIdle(int milliseconds);

	}
}
