#nullable enable

using System;
using Uno.Diagnostics.Eventing;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkTemplatePool
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{266B850B-674C-4D3E-9B58-F680BE653E18}");

			public const int CreateTemplate = 1;
			public const int RecycleTemplate = 2;
			public const int ReuseTemplate = 3;
			public const int ReleaseTemplate = 4;
		}
	}
}
