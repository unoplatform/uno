/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStylesParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Pairs_Length : number;
	public Pairs : Array<string>;
	public static unmarshal(pData:number) : WindowManagerSetStylesParams
	{
		const ret = new WindowManagerSetStylesParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.Pairs_Length = Number(Module.getValue(pData + 4, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 8, "*");
			if(pArray !== 0)
			{
				ret.Pairs = new Array<string>();
				for(var i=0; i<ret.Pairs_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.Pairs.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.Pairs.push(null);
					}
				}
			}
			else
			
			{
				ret.Pairs = null;
			}
		}
		return ret;
	}
}
