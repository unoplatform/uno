using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public async Task When_Linguistic_Alternatives_Immediate_Empty_Ignores_Late_Cancel()
	{
		var sut = new RichEditBox();
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);
		sut.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
		await WindowHelper.WaitForIdle();

		var operation = sut.GetLinguisticAlternativesAsync();
		operation.Cancel();

		try
		{
			var alternatives = await operation;
			Assert.AreEqual(0, alternatives.Count);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
