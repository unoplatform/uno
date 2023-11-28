/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerResetElementBackgroundParams
{
	/* Pack=4 */
	public HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerResetElementBackgroundParams
	{
		const ret = new WindowManagerResetElementBackgroundParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		return ret;
	}
}
