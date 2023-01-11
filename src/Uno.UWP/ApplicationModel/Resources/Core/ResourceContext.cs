namespace Windows.ApplicationModel.Resources.Core
{
	public partial class ResourceContext
	{
		public ResourceContext()
		{
		}

		public static ResourceContext GetForCurrentView() => new ResourceContext();

		public static ResourceContext GetForViewIndependentUse() => new ResourceContext();

		public global::System.Collections.Generic.IReadOnlyList<string> Languages
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> ResourceContext.Languages is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "IReadOnlyList<string> ResourceContext.Languages");
			}
		}
		public global::Windows.Foundation.Collections.IObservableMap<string, string> QualifierValues
		{
			get
			{
				throw new global::System.NotImplementedException("The member IObservableMap<string, string> ResourceContext.QualifierValues is not implemented in Uno.");
			}
		}

		public static ResourceContext GetForUIContext(global::Windows.UI.UIContext context)
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.GetForUIContext(UIContext context) is not implemented in Uno.");
		}

		public void Reset()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.Reset()");
		}

		public void Reset(global::System.Collections.Generic.IEnumerable<string> qualifierNames)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.Reset(IEnumerable<string> qualifierNames)");
		}

		public void OverrideToMatch(global::System.Collections.Generic.IEnumerable<ResourceQualifier> result)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.OverrideToMatch(IEnumerable<ResourceQualifier> result)");
		}

		public ResourceContext Clone()
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.Clone() is not implemented in Uno.");
		}

		public static void SetGlobalQualifierValue(string key, string value, ResourceQualifierPersistence persistence)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.SetGlobalQualifierValue(string key, string value, ResourceQualifierPersistence persistence)");
		}


		public static void SetGlobalQualifierValue(string key, string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.SetGlobalQualifierValue(string key, string value)");
		}
		public static void ResetGlobalQualifierValues()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.ResetGlobalQualifierValues()");
		}
		public static void ResetGlobalQualifierValues(global::System.Collections.Generic.IEnumerable<string> qualifierNames)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.ResetGlobalQualifierValues(IEnumerable<string> qualifierNames)");
		}
		public static ResourceContext CreateMatchingContext(global::System.Collections.Generic.IEnumerable<ResourceQualifier> result)
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.CreateMatchingContext(IEnumerable<ResourceQualifier> result) is not implemented in Uno.");
		}
	}
}
