using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests;

[TestClass]
public class Given_xBindParser
{
	private static XBindRoot Parse(string xBindExpression)
	{
		return new XBindExpressionParser.CoreParser(xBindExpression).ParseXBind();
	}

	[TestMethod]
	public void When_Parsing_Empty_String()
	{
		var root = Parse("");
		// We return null when parsing empty string. Then null is special cased when
		// building the C# expression to generate just the context name
		Assert.IsNull(root);
	}

	[TestMethod]
	public void When_Parsing_Identifier_Single_Character()
	{
		var root = Parse("A");
		Assert.AreEqual("""
			XBindIdentifier
				IdentifierText: A

			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Parsing_Identifier_Multiple_Characters_With_Digits()
	{
		var root = Parse("ABC2");
		Assert.AreEqual("""
			XBindIdentifier
				IdentifierText: ABC2

			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Parsing_Member_Access()
	{
		var root = Parse("A.B");
		Assert.AreEqual("""
			XBindMemberAccess
				Path:
					XBindIdentifier
						IdentifierText: A
				Identifier:
					XBindIdentifier
						IdentifierText: B

			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Parsing_Indexer_Access_Numeric()
	{
		var root = Parse("A.B[0]");
		Assert.AreEqual("""
			XBindIndexerAccess
				Path:
					XBindMemberAccess
						Path:
							XBindIdentifier
								IdentifierText: A
						Identifier:
							XBindIdentifier
								IdentifierText: B
				Index: 0
			
			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Parsing_Indexer_Access_String()
	{
		var root = Parse(@"A.B[""Key""]");
		Assert.AreEqual("""
			XBindIndexerAccess
				Path:
					XBindMemberAccess
						Path:
							XBindIdentifier
								IdentifierText: A
						Identifier:
							XBindIdentifier
								IdentifierText: B
				Index: "Key"

			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Parsing_Cast_Followed_By_Member_Access()
	{
		var root = Parse("((TextBox)obj).Text");
		Assert.AreEqual("""
			XBindMemberAccess
				Path:
					XBindParenthesizedExpression
						Expression:
							XBindCast
								Type:
									XBindIdentifier
										IdentifierText: TextBox
								Expression:
									XBindIdentifier
										IdentifierText: obj
						IsPathlessCast: False
				Identifier:
					XBindIdentifier
						IdentifierText: Text

			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Parsing_Pathless_Cast()
	{
		var root = Parse("(global::System.String)");
		Assert.AreEqual("""
			XBindParenthesizedExpression
				Expression:
					XBindMemberAccess
						Path:
							XBindIdentifier
								IdentifierText: global::System
						Identifier:
							XBindIdentifier
								IdentifierText: String
				IsPathlessCast: True

			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Parsing_Pathless_Cast_In_Argument()
	{
		var root = Parse("Method((global::System.String))");
		Assert.AreEqual("""
			XBindInvocation
				Path:
					XBindIdentifier
						IdentifierText: Method
				Arguments:
					XBindPathArgument
						Path:
							XBindParenthesizedExpression
								Expression:
									XBindMemberAccess
										Path:
											XBindIdentifier
												IdentifierText: global::System
										Identifier:
											XBindIdentifier
												IdentifierText: String
								IsPathlessCast: True

			""", root.PrettyPrint());
	}
}
