/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementTransformParams
{
	/* Pack=8 */
	ScaleX : number;
	ScaleY : number;
	TranslateX : number;
	TranslateY : number;
	HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerSetElementTransformParams
	{
		let ret = new WindowManagerSetElementTransformParams();
		
		{
			ret.ScaleX = Number(Module.getValue(pData + 0, "double"));
		}
		
		{
			ret.ScaleY = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.TranslateX = Number(Module.getValue(pData + 16, "double"));
		}
		
		{
			ret.TranslateY = Number(Module.getValue(pData + 24, "double"));
		}
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 32, "*"));
		}
		return ret;
	}
}
