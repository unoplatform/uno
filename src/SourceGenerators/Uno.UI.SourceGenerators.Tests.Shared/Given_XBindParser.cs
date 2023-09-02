using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.SourceGenerators.Tests;

[TestClass]
public class Given_XBindParser
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

	[TestMethod]
	public void When_Invocation_From_Identifier()
	{
		var root = Parse("SingleTypeProperty.ToUpper()");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindIdentifier
									IdentifierText: SingleTypeProperty
							Identifier:
								XBindIdentifier
									IdentifierText: ToUpper
					Arguments:

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Invocation_From_Member_Access()
	{
		var root = Parse("SingleTypeProperty.A.ToUpper()");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindMemberAccess
									Path:
										XBindIdentifier
											IdentifierText: SingleTypeProperty
									Identifier:
										XBindIdentifier
											IdentifierText: A
							Identifier:
								XBindIdentifier
									IdentifierText: ToUpper
					Arguments:

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Invocation_With_Three_Arguments()
	{
		var root = Parse("Static.TestFunction3(TypeProperty1.InnerProp.InnerInnerProp, TypeProperty2.InnerProp)");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindIdentifier
									IdentifierText: Static
							Identifier:
								XBindIdentifier
									IdentifierText: TestFunction3
					Arguments:
						XBindPathArgument
							Path:
								XBindMemberAccess
									Path:
										XBindMemberAccess
											Path:
												XBindIdentifier
													IdentifierText: TypeProperty1
											Identifier:
												XBindIdentifier
													IdentifierText: InnerProp
									Identifier:
										XBindIdentifier
											IdentifierText: InnerInnerProp
						XBindPathArgument
							Path:
								XBindMemberAccess
									Path:
										XBindIdentifier
											IdentifierText: TypeProperty2
									Identifier:
										XBindIdentifier
											IdentifierText: InnerProp

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Invocation_With_Two_Arguments()
	{
		var root = Parse("Static.TestFunction3(TypeProperty1.InnerProp, TypeProperty2)");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindIdentifier
									IdentifierText: Static
							Identifier:
								XBindIdentifier
									IdentifierText: TestFunction3
					Arguments:
						XBindPathArgument
							Path:
								XBindMemberAccess
									Path:
										XBindIdentifier
											IdentifierText: TypeProperty1
									Identifier:
										XBindIdentifier
											IdentifierText: InnerProp
						XBindPathArgument
							Path:
								XBindIdentifier
									IdentifierText: TypeProperty2

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Invocation_With_One_Argument()
	{
		var root = Parse("Static.TestFunction(TypeProperty1)");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindIdentifier
									IdentifierText: Static
							Identifier:
								XBindIdentifier
									IdentifierText: TestFunction
					Arguments:
						XBindPathArgument
							Path:
								XBindIdentifier
									IdentifierText: TypeProperty1

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Invocation_With_Two_Identifier_Argument()
	{
		var root = Parse("Static.TestFunction2(TypeProperty1, TypeProperty2)");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindIdentifier
									IdentifierText: Static
							Identifier:
								XBindIdentifier
									IdentifierText: TestFunction2
					Arguments:
						XBindPathArgument
							Path:
								XBindIdentifier
									IdentifierText: TypeProperty1
						XBindPathArgument
							Path:
								XBindIdentifier
									IdentifierText: TypeProperty2

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Invocation_Static_With_One_Identifier_Argument()
	{
		var root = Parse("global::Static.TestFunction2(TypeProperty1)");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindIdentifier
									IdentifierText: global::Static
							Identifier:
								XBindIdentifier
									IdentifierText: TestFunction2
					Arguments:
						XBindPathArgument
							Path:
								XBindIdentifier
									IdentifierText: TypeProperty1

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Invocation_From_Identifier_With_Two_Identifier_Argument()
	{
		var root = Parse("Max(TypeProperty1, TypeProperty2)");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindIdentifier
							IdentifierText: Max
					Arguments:
						XBindPathArgument
							Path:
								XBindIdentifier
									IdentifierText: TypeProperty1
						XBindPathArgument
							Path:
								XBindIdentifier
									IdentifierText: TypeProperty2

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Indexer_Followed_By_Access()
	{
		var root = Parse("A.B[0].C");
		Assert.AreEqual("""
				XBindMemberAccess
					Path:
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
					Identifier:
						XBindIdentifier
							IdentifierText: C

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Indexer_Followed_By_Two_Accesses()
	{
		var root = Parse("A.B[0].C.D");
		Assert.AreEqual("""
				XBindMemberAccess
					Path:
						XBindMemberAccess
							Path:
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
							Identifier:
								XBindIdentifier
									IdentifierText: C
					Identifier:
						XBindIdentifier
							IdentifierText: D

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Indexer_In_Invocation_Path_And_Argument()
	{
		var root = Parse("A.B[0].C.D(A.B[0])");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindMemberAccess
									Path:
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
									Identifier:
										XBindIdentifier
											IdentifierText: C
							Identifier:
								XBindIdentifier
									IdentifierText: D
					Arguments:
						XBindPathArgument
							Path:
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
	public void When_Indexer_In_Invocation_Path_And_Argument_Complex()
	{
		var root = Parse("A.B[0].C.D(A.B[0].C.D)");
		Assert.AreEqual("""
				XBindInvocation
					Path:
						XBindMemberAccess
							Path:
								XBindMemberAccess
									Path:
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
									Identifier:
										XBindIdentifier
											IdentifierText: C
							Identifier:
								XBindIdentifier
									IdentifierText: D
					Arguments:
						XBindPathArgument
							Path:
								XBindMemberAccess
									Path:
										XBindMemberAccess
											Path:
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
											Identifier:
												XBindIdentifier
													IdentifierText: C
									Identifier:
										XBindIdentifier
											IdentifierText: D

				""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Chained_AttachedProperties()
	{
		var root = Parse("instance.(AttachedProps.P1).(AttachedProps.P2)");
		Assert.AreEqual("""
			XBindAttachedPropertyAccess
				Member:
					XBindAttachedPropertyAccess
						Member:
							XBindIdentifier
								IdentifierText: instance
						PropertyClass:
							XBindIdentifier
								IdentifierText: AttachedProps
						PropertyName:
							XBindIdentifier
								IdentifierText: P1
				PropertyClass:
					XBindIdentifier
						IdentifierText: AttachedProps
				PropertyName:
					XBindIdentifier
						IdentifierText: P2

			""", root.PrettyPrint());
	}

	[TestMethod]
	public void When_Chained_AttachedProperties_Globalized()
	{
		var root = Parse("instance.(global::NS.AttachedProps.P1).(global::NS.AttachedProps.P2)");
		Assert.AreEqual("""
			XBindAttachedPropertyAccess
				Member:
					XBindAttachedPropertyAccess
						Member:
							XBindIdentifier
								IdentifierText: instance
						PropertyClass:
							XBindMemberAccess
								Path:
									XBindIdentifier
										IdentifierText: global::NS
								Identifier:
									XBindIdentifier
										IdentifierText: AttachedProps
						PropertyName:
							XBindIdentifier
								IdentifierText: P1
				PropertyClass:
					XBindMemberAccess
						Path:
							XBindIdentifier
								IdentifierText: global::NS
						Identifier:
							XBindIdentifier
								IdentifierText: AttachedProps
				PropertyName:
					XBindIdentifier
						IdentifierText: P2

			""", root.PrettyPrint());
	}
}
