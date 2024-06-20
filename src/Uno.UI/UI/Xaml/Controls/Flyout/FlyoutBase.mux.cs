using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	/// <summary>
	/// Attempts to invoke a keyboard shortcut (accelerator).
	/// </summary>
	/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
	public void TryInvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args) => OnProcessKeyboardAccelerators(args);

	/// <summary>
	/// Called just before a keyboard shortcut (accelerator) is processed in your app. Invoked whenever application code or internal 
	/// processes call ProcessKeyboardAccelerators. Override this method to influence the default accelerator handling.
	/// </summary>
	/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
	protected virtual void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
	}

	internal void SetIsWindowedPopup()
	{
		// TODO: Uno
		//HRESULT hr = S_OK;

		//IFC(EnsurePopupAndPresenter());

		//// Set popup to be windowed, if we can, in order to support rendering the popup out of the XAML window.
		//if (CPopup::DoesPlatformSupportWindowedPopup(DXamlCore::GetCurrent()->GetHandle()))
		//{
		//	// Set popup to be windowed, if we can, in order to support rendering the popup out of the XAML window.
		//	if (m_tpPopup && !static_cast<CPopup*>(m_tpPopup.Cast<Popup>()->GetHandle())->IsWindowed())
		//	{
		//		CPopup* popup = static_cast<CPopup*>(m_tpPopup.Cast<Popup>()->GetHandle());

		//		if (!popup->WasEverOpened())
		//		{
		//			IFC(popup->SetIsWindowed());
		//			global::System.Diagnostics.Debug.Assert(popup.IsWindowed());
		//		}
		//	}
		//}
	}

	private bool IsWindowedPopup()
	{
		return false;
		// TODO: Uno
		//bool areWindowedPopupsSupported = CPopup::DoesPlatformSupportWindowedPopup(DXamlCore::GetCurrent()->GetHandle());
		//bool isPopupWindowed = m_tpPopup ? static_cast<CPopup*>(m_tpPopup.Cast<Popup>()->GetHandle())->IsWindowed() : false;

		//return areWindowedPopupsSupported && (isPopupWindowed || m_openingWindowedInProgress);
	}
}
