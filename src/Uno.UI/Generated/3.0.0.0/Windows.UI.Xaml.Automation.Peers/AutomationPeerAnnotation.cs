#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class AutomationPeerAnnotation : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Automation.AnnotationType Type
		{
			get
			{
				return (global::Windows.UI.Xaml.Automation.AnnotationType)this.GetValue(TypeProperty);
			}
			set
			{
				this.SetValue(TypeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Automation.Peers.AutomationPeer Peer
		{
			get
			{
				return (global::Windows.UI.Xaml.Automation.Peers.AutomationPeer)this.GetValue(PeerProperty);
			}
			set
			{
				this.SetValue(PeerProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PeerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Peer), typeof(global::Windows.UI.Xaml.Automation.Peers.AutomationPeer), 
			typeof(global::Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Automation.Peers.AutomationPeer)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TypeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Type), typeof(global::Windows.UI.Xaml.Automation.AnnotationType), 
			typeof(global::Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Automation.AnnotationType)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public AutomationPeerAnnotation( global::Windows.UI.Xaml.Automation.AnnotationType type) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation", "AutomationPeerAnnotation.AutomationPeerAnnotation(AnnotationType type)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.AutomationPeerAnnotation(Windows.UI.Xaml.Automation.AnnotationType)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public AutomationPeerAnnotation( global::Windows.UI.Xaml.Automation.AnnotationType type,  global::Windows.UI.Xaml.Automation.Peers.AutomationPeer peer) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation", "AutomationPeerAnnotation.AutomationPeerAnnotation(AnnotationType type, AutomationPeer peer)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.AutomationPeerAnnotation(Windows.UI.Xaml.Automation.AnnotationType, Windows.UI.Xaml.Automation.Peers.AutomationPeer)
		// Skipping already declared method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.AutomationPeerAnnotation()
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.AutomationPeerAnnotation()
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.Type.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.Type.set
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.Peer.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.Peer.set
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.TypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutomationPeerAnnotation.PeerProperty.get
	}
}
