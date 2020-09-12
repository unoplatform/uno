#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.System.UserProfile.GameServices.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GameService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Uri ServiceUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri GameService.ServiceUri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void NotifyPartnerTokenExpired( global::System.Uri audienceUri)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.System.UserProfile.GameServices.Core.GameService", "void GameService.NotifyPartnerTokenExpired(Uri audienceUri)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static uint GetAuthenticationStatus()
		{
			throw new global::System.NotImplementedException("The member uint GameService.GetAuthenticationStatus() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Phone.System.UserProfile.GameServices.Core.GameService.ServiceUri.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.System.UserProfile.GameServices.Core.GameServicePropertyCollection> GetGamerProfileAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GameServicePropertyCollection> GameService.GetGamerProfileAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.System.UserProfile.GameServices.Core.GameServicePropertyCollection> GetInstalledGameItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GameServicePropertyCollection> GameService.GetInstalledGameItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetPartnerTokenAsync( global::System.Uri audienceUri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> GameService.GetPartnerTokenAsync(Uri audienceUri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetPrivilegesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> GameService.GetPrivilegesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void GrantAchievement( uint achievementId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.System.UserProfile.GameServices.Core.GameService", "void GameService.GrantAchievement(uint achievementId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void GrantAvatarAward( uint avatarAwardId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.System.UserProfile.GameServices.Core.GameService", "void GameService.GrantAvatarAward(uint avatarAwardId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void PostResult( uint gameVariant,  global::Windows.Phone.System.UserProfile.GameServices.Core.GameServiceScoreKind scoreKind,  long scoreValue,  global::Windows.Phone.System.UserProfile.GameServices.Core.GameServiceGameOutcome gameOutcome,  global::Windows.Storage.Streams.IBuffer buffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.System.UserProfile.GameServices.Core.GameService", "void GameService.PostResult(uint gameVariant, GameServiceScoreKind scoreKind, long scoreValue, GameServiceGameOutcome gameOutcome, IBuffer buffer)");
		}
		#endif
	}
}
