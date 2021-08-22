/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetPropertyParams
{
	/* Pack=4 */
	public HtmlId : string;
	public Pairs_Length : number;
	public Pairs : Array<string>;
	public static unmarshal(pData:number) : WindowManagerSetPropertyParams
	{
		const ret = new WindowManagerSetPropertyParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.HtmlId = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.HtmlId = null;
			}
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
