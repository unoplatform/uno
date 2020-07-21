#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CertificateEnrollmentManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Certificates.UserCertificateEnrollmentManager UserCertificateEnrollmentManager
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserCertificateEnrollmentManager CertificateEnrollmentManager.UserCertificateEnrollmentManager is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportPfxDataAsync( string pfxData,  string password,  global::Windows.Security.Cryptography.Certificates.PfxImportParameters pfxImportParameters)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CertificateEnrollmentManager.ImportPfxDataAsync(string pfxData, string password, PfxImportParameters pfxImportParameters) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CertificateEnrollmentManager.UserCertificateEnrollmentManager.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportPfxDataAsync( string pfxData,  string password,  global::Windows.Security.Cryptography.Certificates.ExportOption exportable,  global::Windows.Security.Cryptography.Certificates.KeyProtectionLevel keyProtectionLevel,  global::Windows.Security.Cryptography.Certificates.InstallOptions installOption,  string friendlyName,  string keyStorageProvider)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CertificateEnrollmentManager.ImportPfxDataAsync(string pfxData, string password, ExportOption exportable, KeyProtectionLevel keyProtectionLevel, InstallOptions installOption, string friendlyName, string keyStorageProvider) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> CreateRequestAsync( global::Windows.Security.Cryptography.Certificates.CertificateRequestProperties request)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CertificateEnrollmentManager.CreateRequestAsync(CertificateRequestProperties request) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction InstallCertificateAsync( string certificate,  global::Windows.Security.Cryptography.Certificates.InstallOptions installOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CertificateEnrollmentManager.InstallCertificateAsync(string certificate, InstallOptions installOption) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportPfxDataAsync( string pfxData,  string password,  global::Windows.Security.Cryptography.Certificates.ExportOption exportable,  global::Windows.Security.Cryptography.Certificates.KeyProtectionLevel keyProtectionLevel,  global::Windows.Security.Cryptography.Certificates.InstallOptions installOption,  string friendlyName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CertificateEnrollmentManager.ImportPfxDataAsync(string pfxData, string password, ExportOption exportable, KeyProtectionLevel keyProtectionLevel, InstallOptions installOption, string friendlyName) is not implemented in Uno.");
		}
		#endif
	}
}
