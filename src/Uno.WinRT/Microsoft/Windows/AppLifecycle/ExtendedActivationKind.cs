namespace Microsoft.Windows.AppLifecycle;

/// <summary>
/// Defines values that represent activation types.
/// </summary>
public enum ExtendedActivationKind
{
	/// <summary>
	/// The user launched the app or tapped a content tile.
	/// </summary>
	Launch = 0,

	/// <summary>
	/// The user wants to search with the app.
	/// </summary>
	Search = 1,

	/// <summary>
	/// The app is activated as a target for share operations.
	/// </summary>
	ShareTarget = 2,

	/// <summary>
	/// An app launched a file whose file type this app is registered to handle.
	/// </summary>
	File = 3,

	/// <summary>
	/// An app launched a URI whose scheme name this app is registered to handle.
	/// </summary>
	Protocol = 4,

	/// <summary>
	/// The user wants to pick files that are provided by the app.
	/// </summary>
	FileOpenPicker = 5,

	/// <summary>
	/// The user wants to save a file and selected the app as the location.
	/// </summary>
	FileSavePicker = 6,

	/// <summary>
	/// The user wants to save a file that the app provides content management for.
	/// </summary>
	CachedFileUpdater = 7,

	/// <summary>
	/// The user wants to pick contacts.
	/// </summary>
	ContactPicker = 8,

	/// <summary>
	/// The app handles AutoPlay.
	/// </summary>
	Device = 9,

	/// <summary>
	/// The app handles print tasks.
	/// </summary>
	PrintTaskSettings = 10,

	/// <summary>
	/// The app captures photos or video from an attached camera.
	/// </summary>
	CameraSettings = 11,

	/// <summary>
	/// The user launched the restricted app.
	/// </summary>
	RestrictedLaunch = 12,

	/// <summary>
	/// The user wants to manage appointments that are provided by the app.
	/// </summary>
	AppointmentsProvider = 13,

	/// <summary>
	/// The user wants to handle calls or messages for the phone number of a contact that is provided by the app.
	/// </summary>
	Contact = 14,

	/// <summary>
	/// The app launches a call from the lock screen. If the user wants to accept the call, the app displays its call UI directly on the lock screen without requiring the user to unlock. A lock-screen call is a special type of launch activation.
	/// </summary>
	LockScreenCall = 15,

	/// <summary>
	/// The app was activated as the result of a voice command.
	/// </summary>
	VoiceCommand = 16,

	/// <summary>
	/// The app was activated as the lock screen.
	/// </summary>
	LockScreen = 17,

	/// <summary>
	/// Not supported. The app was activated after the completion of a picker.
	/// </summary>
	PickerReturned = 1000,

	/// <summary>
	/// Not supported. The app was activated to perform a Wallet operation.
	/// </summary>
	WalletAction = 1001,

	/// <summary>
	/// Not supported. The app was activated after the app was suspended for a file picker operation.
	/// </summary>
	PickFileContinuation = 1002,

	/// <summary>
	/// Not supported. The app was activated after the app was suspended for a file save picker operation.
	/// </summary>
	PickSaveFileContinuation = 1003,

	/// <summary>
	/// Not supported. The app was activated after the app was suspended for a folder picker operation.
	/// </summary>
	PickFolderContinuation = 1004,

	/// <summary>
	/// Not supported. The app was activated after the app was suspended for a web authentication broker operation.
	/// </summary>
	WebAuthenticationBrokerContinuation = 1005,

	/// <summary>
	/// The app was activated by a web account provider.
	/// </summary>
	WebAccountProvider = 1006,

	/// <summary>
	/// Reserved for system use.
	/// </summary>
	ComponentUI = 1007,

	/// <summary>
	/// The app was launched by another app with the expectation that it will return a result back to the caller.
	/// </summary>
	ProtocolForResults = 1009,

	/// <summary>
	/// The app was activated when a user tapped on the body of a toast notification or performed an action inside a toast notification.
	/// </summary>
	ToastNotification = 1010,

	/// <summary>
	/// This app was launched by another app to provide a customized printing experience for a 3D printer.
	/// </summary>
	Print3DWorkflow = 1011,

	/// <summary>
	/// This app was launched by another app on a different device by using the DIAL protocol.
	/// </summary>
	DialReceiver = 1012,

	/// <summary>
	/// This app was activated as a result of pairing a device.
	/// </summary>
	DevicePairing = 1013,

	/// <summary>
	/// The app was launched to handle the user interface for account management.
	/// </summary>
	UserDataAccountsProvider = 1014,

	/// <summary>
	/// Reserved for system use.
	/// </summary>
	FilePickerExperience = 1015,

	/// <summary>
	/// Reserved for system use.
	/// </summary>
	LockScreenComponent = 1016,

	/// <summary>
	/// The app was launched from the My People UI.
	/// </summary>
	ContactPanel = 1017,

	/// <summary>
	/// The app was activated because the user is printing to a printer that has a Print Workflow App associated with it which has requested user input.
	/// </summary>
	PrintWorkflowForegroundTask = 1018,

	/// <summary>
	/// The app was activated because it was launched by the OS due to a request for game-specific UI.
	/// </summary>
	GameUIProvider = 1019,

	/// <summary>
	/// The app was activated because the app is specified to launch at system startup or user log-in.
	/// </summary>
	StartupTask = 1020,

	/// <summary>
	/// The app was launched from the command line.
	/// </summary>
	CommandLineLaunch = 1021,

	/// <summary>
	/// The app was activated as a barcode scanner provider.
	/// </summary>
	BarcodeScannerProvider = 1022,

	/// <summary>
	/// The app was activated as print support job UI.
	/// </summary>
	PrintSupportJobUI = 1023,

	/// <summary>
	/// The app was activated as print support settings UI.
	/// </summary>
	PrintSupportSettingsUI = 1024,

	/// <summary>
	/// The app was activated by a phone call.
	/// </summary>
	PhoneCallActivation = 1025,

	/// <summary>
	/// The app was activated as a VPN app in the foreground.
	/// </summary>
	VpnForeground = 1026,

	/// <summary>
	/// The app was activated by a push notification.
	/// </summary>
	Push = 5000,

	/// <summary>
	/// The app was activated by an app notification.
	/// </summary>
	AppNotification = 5001,
}
