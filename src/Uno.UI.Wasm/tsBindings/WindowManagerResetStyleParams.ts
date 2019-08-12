/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerResetStyleParams
{
	/* Pack=4 */
	HtmlId : number;
	Styles_Length : number;
	Styles : Array<string>;
	public static unmarshal(pData:number) : WindowManagerResetStyleParams
	{
		let ret = new WindowManagerResetStyleParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.Styles_Length = Number(Module.getValue(pData + 4, "i32"));
		}
		
		{
			var pArray = Module.getValue(pData + 8, "*");
			if(pArray !== 0)
			{
				ret.Styles = new Array<string>();
				for(var i=0; i<ret.Styles_Length; i++)
				{
					var value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.Styles.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.Styles.push(null);
					}
				}
			}
			else
			
			{
				ret.Styles = null;
			}
		}
		return ret;
	}
}
