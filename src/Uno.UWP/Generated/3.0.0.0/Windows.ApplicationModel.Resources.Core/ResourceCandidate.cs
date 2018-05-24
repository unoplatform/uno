#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Resources.Core
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResourceCandidate 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsDefault
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ResourceCandidate.IsDefault is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsMatch
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ResourceCandidate.IsMatch is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsMatchAsDefault
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ResourceCandidate.IsMatchAsDefault is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Resources.Core.ResourceQualifier> Qualifiers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ResourceQualifier> ResourceCandidate.Qualifiers is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string ValueAsString
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ResourceCandidate.ValueAsString is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceCandidate.Qualifiers.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceCandidate.IsMatch.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceCandidate.IsMatchAsDefault.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceCandidate.IsDefault.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceCandidate.ValueAsString.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetValueAsFileAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> ResourceCandidate.GetValueAsFileAsync() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string GetQualifierValue( string qualifierName)
		{
			throw new global::System.NotImplementedException("The member string ResourceCandidate.GetQualifierValue(string qualifierName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> GetValueAsStreamAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> ResourceCandidate.GetValueAsStreamAsync() is not implemented in Uno.");
		}
		#endif
	}
}
