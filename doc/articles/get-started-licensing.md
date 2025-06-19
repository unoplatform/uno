---
uid: Uno.GetStarted.Licensing
---

# Sign in with Uno Platform

Sign in with your Uno Platform account directly in your favorite IDE (Visual Studio, VS Code, or Rider), to unlock powerful tools like Hot Reload, helping you speed up development. With a single registration, you also get early access to new features and the opportunity to connect with the Uno Platform community to share feedback and network.

## Create your account

1. Go to our website, [platform.uno](https://platform.uno/), and click on the **Sign in** button in the top right corner, or go directly to [platform.uno/my-account](https://platform.uno/my-account).
1. Enter your email address and click on **Register**.
1. On the registration page, fill in your information. Once done, click on **Sign up**.
1. You will receive a confirmation email from `no-reply@platform.uno`. Follow the instructions in the email to activate your account.
1. You should then see the sign-in page. Enter your email and password and click on **Sign in** to access your account details, where you can update information or add more details.

## Sign in to your IDE of choice

After creating your Uno Platform account, follow the steps below to sign in to your preferred IDE:

**I am developing on...**

### [**Visual Studio 2022**](#tab/vs2022)

If you’ve already set up **Visual Studio 2022** by following the [Get Started on Visual Studio 2022](xref:Uno.GetStarted.vs2022) documentation, sign in as follows:

1. Create a new Uno Platform project by following the [Creating an app with Visual Studio 2022](xref:Uno.GettingStarted.CreateAnApp.VS2022) documentation or open an existing one with Uno.Sdk version 5.5 or higher.

   For existing applications, you should take this opportunity to update to the [latest `Uno.Sdk` version](https://www.nuget.org/packages/Uno.Sdk). See our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) to upgrade.

1. After your project has finished loading, a notification should appear. Click on the **Sign in / Register** button.

   ![Visual Studio 2022 notification](Assets/uno-settings-notification.png)

   > [!TIP]
   > Ensure that the lower left IDE icon shows a check mark and says "Ready" ![A checkmark with a text saying ready](getting-started/wizard/assets/vs2022-ready-statusbar.png). This ensures that the projects have been created, and their dependencies have been restored completely.
   >
   > [!NOTE]
   > If the notification doesn’t appear, access the Studio app by clicking on **Extensions** > **Uno Platform** > **Studio...**.
   >
   > ![Visual Studio 2022 Menu](Assets/uno-settings-vs.png)

### [**Visual Studio Code**](#tab/vscode)

If you’ve already set up **Visual Studio Code** by following the [Get Started on VS Code](xref:Uno.GetStarted.vscode) documentation, sign in as follows:

1. Create a new Uno Platform project by following the [Creating an app with VS Code](xref:Uno.GettingStarted.CreateAnApp.VSCode) documentation or open an existing one with Uno.Sdk version 5.5 or higher.

   For existing applications, you should take this opportunity to update to the [latest `Uno.Sdk` version](https://www.nuget.org/packages/Uno.Sdk). See our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) to upgrade.

1. After your project has finished loading, check the status bar at the bottom left of VS Code. By default, `YourProjectName.sln` is selected. Switch to `YourProjectName.csproj` to load the project instead.

   ![Selecting the proper project in Visual Studio Code](Assets/uno-settings-vsc-select-project.png)

1. Once you've switched to the proper project, a notification should appear. Click the **Sign in / Register** button.

   ![Visual Studio Code notification](Assets/uno-settings-vsc-notification.png)

   > [!NOTE]
   > If the notification doesn’t appear, access the Studio app by selecting **View** > **Command Palette...** and typing `Uno Platform: Open Studio`.
   >
   > ![Visual Studio Code Menu](Assets/uno-settings-vsc.jpeg)

### [**JetBrains Rider**](#tab/rider)

If you’ve already set up **JetBrains Rider** by following the [Get Started on JetBrains Rider](xref:Uno.GetStarted.Rider) documentation, sign in as follows:

1. Create a new Uno Platform project by following the [Create an app with JetBrains Rider](xref:Uno.GettingStarted.CreateAnApp.Rider) documentation or open an existing one with Uno.Sdk version 5.5 or higher.

   For existing applications, you should take this opportunity to update to the [latest `Uno.Sdk` version](https://www.nuget.org/packages/Uno.Sdk). See our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) to upgrade.

1. After your project has finished loading, a notification should appear. Click on the **Sign in / Register** button.

   ![JetBrains Rider notification](Assets/uno-settings-rider-notification.png)

   > [!NOTE]
   > If the notification doesn’t appear, access the Studio app by selecting **Tools** > **Uno Platform** > **Studio...**.
   >
   > ![JetBrains Rider Menu](Assets/uno-settings-rider.png)

---

### Uno Platform Studio window

1. In the Uno Platform Studio window, click on **Sign in**. You’ll be redirected to your browser to enter your Uno Platform account credentials.

   ![Uno Platform Studio window Welcome](Assets/uno-studio-welcome.png)

1. Once signed in, you’ll see a confirmation of your account along with your license details.

   You can then use the **Hot Reload** feature to speed up your workflow and test changes in real-time. For more information, refer to the [Hot Reload documentation](xref:Uno.Features.HotReload).

   ![Uno Platform Studio window signed in](Assets/uno-studio-main.png)

   > [!TIP]
   > You can also access a menu where you can select **My Account** to view your account details, **Refresh** the account changes, and **Sign out**.
   >
   > ![Uno Platform Studio window Menu](Assets/uno-studio-menu.png)

1. If you're using a `Uno Platform Studio Pro` license, your Uno Platform Studio window will display additional tools. You can see the comparison below:

   ![Uno Platform Studio window Community vs Pro](Assets/uno-studio-main-com-vs-pro.png)

1. After you are done, feel free to close the Uno Platform Studio window. You can always access it again from your IDE menu by following the steps above.

## Compare Uno Platform Studio Community vs Pro

Discover the additional features available with `Uno Platform Studio Pro`, including Hot Design® - our next-generation visual designer for cross-platform .NET applications.

Check out [Compare Uno Platform Studio Community vs Pro plan](https://platform.uno/select-subscription/) to see what you gain by upgrading.

## Assigning an Uno Platform Studio Pro License

1. Now that you have completed the sign-in process, you will eventually notice, when running your new Templated Uno App in the Debugger, a small Snowflake right beside the Hot Design Flame Icon if you have been under the beta list and your access ended, even though you might have received a Hot Design License already:

  ![cold-HD-image](./studio/Assets/cold-hd-after-beta-end.png)

1. So now enter your Settings again, just like before for logging in for Hot Reload

  ![inactive-hd-image-before-license-assignment](./studio/Assets/inactive-hd-image-before-license-assignment.png Assets/uno-settings-main.png)

   Or alternatively, login in to the Uno Platform Homepage:

  ![find-uno-account-button-on-homepage](./studio/Assets/find-uno-account-button-on-homepage.png)

1. After logging in, click on the left NavigationBar on `Subscriptions` (1), enter your E-Mail Address or the one of the Person, you want to grant access to this License in the Textbox (2) and then on `Assign` (3), which will light up after entering a E-Mail Address beneath it.

  ![active-subscription-image](./studio/Assets/active-subscription-image.png)

1. You will get a Pop-Up, that is asking you to verify you want to assign this License to this E-Mail and telling you that it will send a notification email about the License Assignment to this persons E-Mail Address. Accept this.

1. Now you can close the Browser again and return to your Settings in Visual Studio, click on `Refresh` as before, and you will be greeted with a nice blue Checkmark also on Hot Design letting you know, that you are ready to start your Hot Design Journey!

## Questions

For general questions about Uno Platform, refer to the [general FAQ](xref:Uno.Development.FAQ) or see the [troubleshooting section](xref:Uno.UI.CommonIssues) for common issues and solutions.

If you encounter any issues or need further assistance, join our [Discord server](https://platform.uno/discord), connect with us via [GitHub](https://github.com/unoplatform/uno/discussions), or reach out on our [contact page](https://platform.uno/contact).
