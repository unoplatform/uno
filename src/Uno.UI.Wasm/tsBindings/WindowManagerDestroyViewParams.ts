/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerDestroyViewParams
{
	/* Pack=4 */
	HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerDestroyViewParams
	{
		let ret = new WindowManagerDestroyViewParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		return ret;
	}
}
