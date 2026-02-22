using System;
using Uno;

namespace Windows.ApplicationModel.Activation
{
	/// <summary>
	/// Provides data when an app is activated because it is the app associated with a URI scheme name.
	/// </summary>
	public partial class ProtocolActivatedEventArgs :
		IProtocolActivatedEventArgs,
		IActivatedEventArgs,
		IProtocolActivatedEventArgsWithCallerPackageFamilyNameAndData,
		IApplicationViewActivatedEventArgs,
		IViewSwitcherProvider,
		IActivatedEventArgsWithUser
	{
		internal ProtocolActivatedEventArgs()
		{
		}

		/// <summary>
		/// Internal-only constructor for protocol activation.
		/// </summary>
		/// <param name="uri">Activated uri.</param>
		/// <param name="previousExecutionState">Previous execution state.</param>
		internal ProtocolActivatedEventArgs(Uri uri, ApplicationExecutionState previousExecutionState)
		{
			Uri = uri;
			PreviousExecutionState = previousExecutionState;
		}

		/// <summary>
		/// Gets the activation type.
		/// </summary>
		public ActivationKind Kind => ActivationKind.Protocol;

		/// <summary>
		/// Gets the execution state of the app before it was activated.
		/// </summary>
		public ApplicationExecutionState PreviousExecutionState { get; }

		/// <summary>
		/// Gets the Uniform Resource Identifier (URI) for which the app was activated.
		/// </summary>
		public Uri Uri { get; }

		[NotImplemented]
		public SplashScreen SplashScreen
		{
			get;
		}

		[NotImplemented]
		public int CurrentlyShownApplicationViewId
		{
			get;
		}
	}
}
