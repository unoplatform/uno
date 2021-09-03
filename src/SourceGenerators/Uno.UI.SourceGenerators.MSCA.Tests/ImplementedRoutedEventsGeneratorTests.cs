using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.SourceGenerators.ImplementedRoutedEvents;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests
{
	using Verify = CSharpSourceGeneratorVerifier<ImplementedRoutedEventsGenerator>;

	[TestClass]
	public class ImplementedRoutedEventsGeneratorTests
	{
		private const string ControlStub = @"
namespace Windows.UI.Xaml.Controls
{
	public class Control
	{
		protected virtual RoutedEventFlag GetImplementedRoutedEvents()
		{
			throw new System.NotImplementedException();
		}
	}
}";
		[TestMethod]
		public async Task Test()
		{
			const string inputSource = @"
using Windows.UI.Xaml.Controls;

public partial class MyAwesomeControl : Control
{
}
";
			await new Verify.Test
			{
				TestState =
				{
					Sources = { ControlStub, inputSource },
					GeneratedSources =
					{
					},
				},
			}.RunAsync();
		}
	}
}
