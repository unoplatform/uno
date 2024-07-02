using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Xaml.Markup
{
	internal partial class XamlServiceProviderContext : IXamlServiceProvider, IProvideValueTarget, IRootObjectProvider, IUriContext, IXamlTypeResolver
	{
		// On uwp and winui, all these interfaces are found on the same class.
		// In fact, you can just type-cast it instead of accessing via IXamlServiceProvider.GetService(Type)
		private static readonly HashSet<Type> SupportedInterfaces = new()
		{
			// typeof(IXamlServiceProvider), // should return null
			typeof(IProvideValueTarget),
			//typeof(IRootObjectProvider), // TODO
			//typeof(IUriContext), // TODO
			//typeof(IXamlTypeResolver), // TODO
		};

		object IXamlServiceProvider.GetService(Type type) => SupportedInterfaces.Contains(type) ? this : null;

		object IRootObjectProvider.RootObject => throw new NotImplementedException();

		public object TargetObject { get; set; }

		public object TargetProperty { get; set; }

		Uri IUriContext.BaseUri => throw new NotImplementedException();

		Type IXamlTypeResolver.Resolve(string qualifiedTypeName) => throw new NotImplementedException();
	}
}
