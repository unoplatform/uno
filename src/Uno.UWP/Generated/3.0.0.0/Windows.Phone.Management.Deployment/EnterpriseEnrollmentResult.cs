#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EnterpriseEnrollmentResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.Management.Deployment.Enterprise EnrolledEnterprise
		{
			get
			{
				throw new global::System.NotImplementedException("The member Enterprise EnterpriseEnrollmentResult.EnrolledEnterprise is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.Management.Deployment.EnterpriseEnrollmentStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member EnterpriseEnrollmentStatus EnterpriseEnrollmentResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Phone.Management.Deployment.EnterpriseEnrollmentResult.EnrolledEnterprise.get
		// Forced skipping of method Windows.Phone.Management.Deployment.EnterpriseEnrollmentResult.Status.get
	}
}
