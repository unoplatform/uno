using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation
{
	public enum ActivationKind
	{
		//
		// Summary:
		//     The user launched the app or tapped a content tile.
		Launch = 0,
		//
		// Summary:
		//     The user wants to search with the app.
		Search = 1,
		//
		// Summary:
		//     The app is activated as a target for share operations.
		ShareTarget = 2,
		//
		// Summary:
		//     An app launched a file whose file type this app is registered to handle.
		File = 3,
		//
		// Summary:
		//     An app launched a URI whose scheme name this app is registered to handle.
		Protocol = 4,
		//
		// Summary:
		//     The user wants to pick files that are provided by the app.
		FileOpenPicker = 5,
		//
		// Summary:
		//     The user wants to save a file and selected the app as the location.
		FileSavePicker = 6,
		//
		// Summary:
		//     The user wants to save a file that the app provides content management for.
		CachedFileUpdater = 7,
		//
		// Summary:
		//     The user wants to pick contacts.
		ContactPicker = 8,
		//
		// Summary:
		//     The app handles AutoPlay.
		Device = 9,
		//
		// Summary:
		//     The app handles print tasks.
		PrintTaskSettings = 10,
		//
		// Summary:
		//     The app captures photos or video from an attached camera.
		CameraSettings = 11,
		//
		// Summary:
		//     Windows Store only. The user launched the restricted app.
		RestrictedLaunch = 12,
		//
		// Summary:
		//     The user wants to manage appointments that are provided by the app.
		AppointmentsProvider = 13,
		//
		// Summary:
		//     Windows Store only. The user wants to handle calls or messages for the phone
		//     number of a contact that is provided by the app.
		Contact = 14,
		//
		// Summary:
		//     Windows Store only. The app launches a call from the lock screen. If the user
		//     wants to accept the call, the app displays its call UI directly on the lock screen
		//     without requiring the user to unlock. A lock-screen call is a special type of
		//     launch activation.
		LockScreenCall = 15,
		//
		// Summary:
		//     The app was activated as the result of a voice command.Not supported in Windows
		//     8 and Windows 8.1 apps.
		VoiceCommand = 16,
		//
		// Summary:
		//     The app was activated as the lock screen. Introduced in Windows 10.
		LockScreen = 17,
		//
		// Summary:
		//     Windows Phone only. The app was activated after the completion of a picker.
		PickerReturned = 1000,
		//
		// Summary:
		//     Windows Phone only. The app was activated to perform a Wallet operation.
		WalletAction = 1001,
		//
		// Summary:
		//     Windows Phone only. The app was activated after the app was suspended for a file
		//     picker operation.
		PickFileContinuation = 1002,
		//
		// Summary:
		//     Windows Phone only. The app was activated after the app was suspended for a file
		//     save picker operation.
		PickSaveFileContinuation = 1003,
		//
		// Summary:
		//     Windows Phone only. The app was activated after the app was suspended for a folder
		//     picker operation.
		PickFolderContinuation = 1004,
		//
		// Summary:
		//     Windows Phone only. The app was activated after the app was suspended for a web
		//     authentication broker operation.
		WebAuthenticationBrokerContinuation = 1005,
		//
		// Summary:
		//     The app was activated by a web account provider. Introduced in Windows 10.
		WebAccountProvider = 1006,
		//
		// Summary:
		//     Reserved for system use. Introduced in Windows 10.
		ComponentUI = 1007,
		//
		// Summary:
		//     The app was launched by another app with the expectation that it will return
		//     a result back to the caller. Introduced in Windows 10.
		ProtocolForResults = 1009,
		//
		// Summary:
		//     The app was activated when a user tapped on the body of a toast notification
		//     or performed an action inside a toast notification. Introduced in Windows 10.
		ToastNotification = 1010,
		//
		// Summary:
		//     This app was launched by another app to provide a customized printing experience
		//     for a 3D printer. Introduced in Windows 10.
		Print3DWorkflow = 1011,
		//
		// Summary:
		//     This app was launched by another app on a different device by using the DIAL
		//     protocol. Introduced in Windows 10.
		DialReceiver = 1012,
		//
		// Summary:
		//     This app was activated as a result of pairing a device.
		DevicePairing = 1013,

		//
		// Summary:
		//     The app was launched to handle the user interface for account management. Introduced in Windows 10, version 1607 (10.0.14393).
		UserDataAccountsProvider = 1014,

		//
		// Summary:
		//     Reserved for system use. Introduced in Windows 10, version 1607 (10.0.14393).
		FilePickerExperience = 1015,

		//
		// Summary:
		//     Reserved for system use. Introduced in Windows 10, version 1703 (10.0.15063).
		LockScreenComponent = 1016,

		//
		// Summary:
		//     The app was launched from the My People UI. Note: introduced in Windows 10, version 1703 (10.0.15063), but not used. Now used starting with Windows 10, version 1709 (10.0.16299).
		ContactPanel = 1017,

		//
		// Summary:
		//     The app was activated because the user is printing to a printer that has a Print Workflow Application associated with it which has requested user input.
		PrintWorkflowForegroundTask = 1018,

		//
		// Summary:
		//    The app was activated because it was launched by the OS due to a game's request for Xbox-specific UI. Introduced in Windows 10, version 1703 (10.0.15063).
		GameUIProvider = 1019,

		//
		// Summary:
		//    The app was activated because the app is specified to launch at system startup or user log-in. Introduced in Windows 10, version 1703 (10.0.15063).
		StartupTask = 1020,

		//
		// Summary:
		//    The app was launched from the command line. Introduced in Windows 10, version 1709 (10.0.16299) 
		CommandLineLaunch = 1021,

		//
		// Summary:
		//     The app was activated as a barcode scanner provider.
		BarcodeScannerProvider = 1022	
	}
}
