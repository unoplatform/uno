/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementTransformParams
{
	/* Pack=8 */
	public HtmlId : number;
	public M11 : number;
	public M12 : number;
	public M21 : number;
	public M22 : number;
	public M31 : number;
	public M32 : number;
	public ClipToBounds : boolean;
	public static unmarshal(pData:number) : WindowManagerSetElementTransformParams
	{
		const ret = new WindowManagerSetElementTransformParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.M11 = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.M12 = Number(Module.getValue(pData + 16, "double"));
		}
		
		{
			ret.M21 = Number(Module.getValue(pData + 24, "double"));
		}
		
		{
			ret.M22 = Number(Module.getValue(pData + 32, "double"));
		}
		
		{
			ret.M31 = Number(Module.getValue(pData + 40, "double"));
		}
		
		{
			ret.M32 = Number(Module.getValue(pData + 48, "double"));
		}
		
		{
			ret.ClipToBounds = Boolean(Module.getValue(pData + 56, "i32"));
		}
		return ret;
	}
}
