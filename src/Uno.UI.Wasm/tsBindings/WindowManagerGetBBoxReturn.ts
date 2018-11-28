/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxReturn
{
	/* Pack=8 */
	X : number;
	Y : number;
	Width : number;
	Height : number;
	public marshal(pData:number)
	{
		Module.setValue(pData + 0, this.X, "double");
		Module.setValue(pData + 8, this.Y, "double");
		Module.setValue(pData + 16, this.Width, "double");
		Module.setValue(pData + 24, this.Height, "double");
	}
}
