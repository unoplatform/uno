#if !NETFX_CORE
using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace Uno.UI.DataBinding
{

	internal static partial class BindingPropertyHelper
	{
		[Preserve]
		[RequiresDynamicCode("`System.Dynamic` use requires dynamic code.")]
		private class UnoGetMemberBinder : GetMemberBinder
		{
			public UnoGetMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase) { }

			public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
				=> throw new NotSupportedException();
		}

		[Preserve]
		[RequiresDynamicCode("`System.Dynamic` use requires dynamic code.")]
		private class UnoSetMemberBinder : SetMemberBinder
		{
			public UnoSetMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase) { }

			public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
				=> throw new NotImplementedException();
		}
	}
}
#endif
