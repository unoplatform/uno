/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementTransformParams
{
	/* Pack=8 */
	public M11 : number;
	public M12 : number;
	public M21 : number;
	public M22 : number;
	public M31 : number;
	public M32 : number;
	public HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerSetElementTransformParams
	{
		const ret = new WindowManagerSetElementTransformParams();
		
		{
			ret.M11 = Number(Module.getValue(pData + 0, "double"));
		}
		
		{
			ret.M12 = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.M21 = Number(Module.getValue(pData + 16, "double"));
		}
		
		{
			ret.M22 = Number(Module.getValue(pData + 24, "double"));
		}
		
		{
			ret.M31 = Number(Module.getValue(pData + 32, "double"));
		}
		
		{
			ret.M32 = Number(Module.getValue(pData + 40, "double"));
		}
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 48, "*"));
		}
		return ret;
	}
}
