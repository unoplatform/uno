namespace Windows.Foundation.Metadata;

/// <summary>
/// Expresses the state of the Windows Runtime feature associated with a Windows Runtime Type.
/// </summary>
[AttributeUsage(
	AttributeTargets.Delegate |
	AttributeTargets.Enum |
	AttributeTargets.Field |
	AttributeTargets.Interface |
	AttributeTargets.Method |
	AttributeTargets.Class |
	AttributeTargets.Struct)]
public partial class FeatureAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="featureStage">Specifies if the named feature is enabled or disabled.</param>
	/// <param name="validInAllBranches">Boolean value that indicates if the named feature is valid in all branches.</param>
	public FeatureAttribute(FeatureStage featureStage, bool validInAllBranches) : base()
	{
	}
}
