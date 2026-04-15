#nullable enable

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests_CSharpExpressions.Pages;

namespace Uno.UI.RuntimeTests.Tests_CSharpExpressions;

/// <summary>
/// T027 (US1) — runtime tests for compound C# expression lowering on
/// <see cref="CompoundExpressionPage"/>. Compound expressions are lowered to a
/// synthesized <c>private ReturnType __xcs_Expr_NNN() => ...;</c> helper on the page
/// partial and referenced via a <c>{x:Bind __xcs_Expr_NNN(), Mode=OneWay}</c> binding —
/// reusing the x:Bind pipeline's compiled binding, refresh-set detection, and lifecycle
/// (generated-binding-shapes §3, §4, §7).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_CompoundExpression
{
	[TestMethod]
	public async Task When_ArithmeticCompound_EvaluatesAgainstPageMembers()
	{
		var SUT = new CompoundExpressionPage();
		await UITestHelper.Load(SUT);

		var expected = (SUT.Price * SUT.Quantity).ToString(CultureInfo.CurrentCulture);
		Assert.AreEqual(expected, SUT.TotalLabel.Text);
	}

	[TestMethod]
	public async Task When_CompoundWithTaxRate()
	{
		var SUT = new CompoundExpressionPage();
		await UITestHelper.Load(SUT);

		var expected = (SUT.Price * SUT.TaxRate).ToString(CultureInfo.CurrentCulture);
		Assert.AreEqual(expected, SUT.WithTaxLabel.Text);
	}

	[TestMethod]
	public async Task When_Ternary_SelectsStandardBranchByDefault()
	{
		var SUT = new CompoundExpressionPage();
		await UITestHelper.Load(SUT);

		Assert.AreEqual("Standard", SUT.TierLabel.Text);
	}

	[TestMethod]
	public async Task When_Ternary_SelectsGoldBranchWhenVipOverridden()
	{
		var SUT = new VipCompoundExpressionPage();
		await UITestHelper.Load(SUT);

		Assert.AreEqual("Gold", SUT.TierLabel.Text);
	}
}

public sealed class VipCompoundExpressionPage : CompoundExpressionPage
{
	public override bool IsVip => true;
}
