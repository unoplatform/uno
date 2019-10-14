
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

First, open your app solution in VStudio.
Them using Solution Explorer window
* rename your current Project folder from APPNAME to e.g. APPNAME_OLD.
* add new project (right click on Solution, Add, New Project, Visual C#, Uno Plaform, Cross-Plaform App (Uno Platform), using same name as your previous project (APPNAME). In effect, APPNAME folder would be created, and inside it - folders as APPNAME.Shared, APPNAME.Droid, etc. - one project per one target.

## converting your code
In simple words, you have to copy all your content from APPNAME_OLD to APPNAME.Shared.
All your XAML pages, all code behind it, folders as String and Assets, and all files and folders you created in APPNAME_OLD.
It can be done by either
* recreating files (in Solution Explorer, Add, New) and copying content, or
* copying files in Windows Explorer and Add/Existing file from Solution Explorer.

If your code is in VB, you can use some simple translators, e.g. https://codeconverter.icsharpcode.net/ . It is not perfect translation, some issues you would have to correct manually, but it is a good start.

You have to copy also APPNAME.manifest - especially, app capabilities etc. - but new instance of this file would be in APPNAME.UWP, not in APPNAME.Shared.

Now, you can Unload your APPNAME_OLD project (right click in Solution Explorer). Why? To make VStudio using less memory, start faster, and... to be sure you don't mistakenly change something in your old code.

## check if everything is implemented in Uno
Now, give VStudio some time (in minutes). It will rebuild Intellisense database.
Then try to build your app - not Solution, but only UWP project (choose Debug, and right click APPNAME.UWP, Build).
Look into ErrorList window for warnings "is not implemented in Uno", e.g. "Warning Uno0001 Windows.UI.Xaml.Application.Exit() is not implemented in Uno".

 If you don't have such warnings, you are lucky - and converting app to Platform Uno is probably done.

 Of course, you should test it before uploading to Microsoft Store...
 Build UWP project, check if it is working as expected.

 Then build for another platform (e.g. Android), to check if compilation is ok. Uploading app to Google Store requires much more work, I would cover it in other article.

 But most probably, you would find some Uno0001 warnings. This would be our next topic.
