#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EnterpriseEnrollmentManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Phone.Management.Deployment.Enterprise CurrentEnterprise
		{
			get
			{
				throw new global::System.NotImplementedException("The member Enterprise EnterpriseEnrollmentManager.CurrentEnterprise is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Phone.Management.Deployment.Enterprise> EnrolledEnterprises
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Enterprise> EnterpriseEnrollmentManager.EnrolledEnterprises is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Phone.Management.Deployment.EnterpriseEnrollmentManager.EnrolledEnterprises.get
		// Forced skipping of method Windows.Phone.Management.Deployment.EnterpriseEnrollmentManager.CurrentEnterprise.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ValidateEnterprisesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EnterpriseEnrollmentManager.ValidateEnterprisesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.Management.Deployment.EnterpriseEnrollmentResult> RequestEnrollmentAsync( string enrollmentToken)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EnterpriseEnrollmentResult> EnterpriseEnrollmentManager.RequestEnrollmentAsync(string enrollmentToken) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> RequestUnenrollmentAsync( global::Windows.Phone.Management.Deployment.Enterprise enterprise)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> EnterpriseEnrollmentManager.RequestUnenrollmentAsync(Enterprise enterprise) is not implemented in Uno.");
		}
		#endif
	}
}
