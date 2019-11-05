
# How to contribute to Uno? My experience.


 You are using, or consider use, Platform Uno. So, you are either
* writing new app, and want this app to be multiplatform, or
* you have some UWP app, and want to convert this app to be multiplatform.

 Maybe your programmers experience doesn't include Git, Android, iOS, etc. Maybe you know only UWP. And maybe even your apps are written in VB... But still, you can not only use Uno, but also contribute to it. Please, read this short guide, guide for beginners.


Assume that you have UWP app, and want to convert it to Uno. What you should do?

In short:
* convert project into Uno,
* find if something you use is not implemented in Uno,
* if yes, you have two options:
	* try to find workaround for this, or
	* extend Uno
* try to run converted app, and test it
* if you encounter some bugs, you have two options:
	* create new Issue [https://github.com/unoplatform/uno/issues] - but check if similar problem is already reported, or
	* try to change Uno code.

## adding new project

First, open your app solution in VStudio. Then using Solution Explorer window:
* add new project (right click on Solution, Add, New Project, Visual C#, Uno Plaform, Cross-Plaform App (Uno Platform), using name as e.g. your previous project (APPNAME) with "\_Uno" suffix. In effect, APPNAME_Uno folder would be created, and inside it - folders as APPNAME_Uno.Shared, APPNAME_Uno.Droid, etc. - one project per one target platform.
* for now, you can Unload your APPNAMEUno.Droid, .iOS and .WASM projects (right click in Solution Explorer). Why? To make VStudio using less memory and start faster.

## converting your code
In simple words, you have to copy all your content from APPNAME to APPNAME_Uno.Shared; all your XAML pages, all code behind it (.cs), and replace folder Strings (delete just generated Strings folders). Copy also all other files and folders you created in APPNAME.
Use Solution Explorer for this.

If your code is in VB, you can use some simple translators, e.g. https://codeconverter.icsharpcode.net/ . It is not perfect translation, some issues you would have to correct manually, but it is a good start. So, in Solution Explorer, for each XAML page:
* open .xaml file from APPNAME,
* open .vb file from APPNAME,
* right click on APPNAME_Uno.Shared, Add, New, C#, XAML, Blank Page - use same name as in APPNAME project,
* open both .xaml and .cs file you just created,
* copy contens of XAML page
* convert .vb code to .cs code, and insert it to .cs page - but do not remove constructor, with `this.InitializeComponent();`. From App.xaml.vb, convert only code you added. Take care of "namespace" - should be same as in .xaml (and as in manifest), without "\_Uno" and "Shared" sufixes.

You have to copy also Package.appxmanifest (especially, app capabilities etc.) and Assets folder - but to APPNAME_Uno.UWP, not to APPNAME_Uno.Shared. 

Now, you can Unload your (old) APPNAME project (right click in Solution Explorer), not only to make VStudio using less memory and start faster, but also to be sure you don't mistakenly change something in your old code.

## check conversion to Uno (UWP)
Try to build your app - not Solution, but only UWP project (choose Debug, and right click APPNAME.UWP, Build). If nothing unexpected happens (no errors), your first step of porting app is done.
Build UWP project, check if it is working as expected.
You can upload new version of your app Microsoft Store. 

## check if everything is implemented in Uno
Now, reload APPNAME_Uno.Droid project (right click on it in Solution Explorer). Give VStudio some time (in minutes). It will rebuild Intellisense database.

Look into ErrorList window for warnings "is not implemented in Uno", e.g. "Warning Uno0001 Windows.UI.Xaml.Application.Exit() is not implemented in Uno".

 If you don't have such warnings, you are lucky - and your app is already 'multiplatformed'.
You can build for another platform (e.g. Android), to check if compilation is ok. Uploading app to Google Store requires much more work, I would cover it in other article.

 But most probably, you would find some Uno0001 warnings. This would be our [next topic](dummies2.md).
