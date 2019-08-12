/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementTransformParams
{
	/* Pack=8 */
	M11 : number;
	M12 : number;
	M21 : number;
	M22 : number;
	M31 : number;
	M32 : number;
	HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerSetElementTransformParams
	{
		let ret = new WindowManagerSetElementTransformParams();
		
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
