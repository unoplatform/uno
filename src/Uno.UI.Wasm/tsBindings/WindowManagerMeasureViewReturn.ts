/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewReturn
{
	/* Pack=8 */
	DesiredWidth : number;
	DesiredHeight : number;
	public serialize(pData:number)
	{
		Module.setValue(pData + 0, this.DesiredWidth, "double");
		Module.setValue(pData + 8, this.DesiredHeight, "double");
	}
}
