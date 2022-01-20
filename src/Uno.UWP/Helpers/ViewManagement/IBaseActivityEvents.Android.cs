#pragma warning disable 618

using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Java.Lang;

namespace Uno.UI.ViewManagement
{
	/// <summary>
	/// A set of events to be raised when key methods are invoked from the main activity
	/// </summary>
	public interface IBaseActivityEvents
	{
		event ActivityActionModeFinishedHandler ActionModeFinished;
		event ActivityActionModeStartedHandler ActionModeStarted;
		event ActivityActivityReenterHandler ActivityReenter;
		event ActivityActivityResultHandler ActivityResult;
		event ActivityAttachedToWindowHandler AttachedToWindow;
		event ActivityAttachFragmentHandler AttachFragment;
		event ActivityBackPressedHandler BackPressed;
		event ActivityChildTitleChangedHandler ChildTitleChanged;
		event ActivityConfigurationChangedHandler ConfigurationChanged;
		event ActivityContentChangedHandler ContentChanged;
		event ActivityContextItemSelectedHandler ContextItemSelected;
		event ActivityContextMenuClosedHandler ContextMenuClosed;
		event ActivityCreateHandler Create;
		event ActivityCreateWithPersistedStateHandler CreateWithPersistedState;
		event ActivityCreateContextMenuHandler CreateContextMenu;
		event ActivityCreateDescriptionFormattedHandler CreateDescriptionFormatted;
		event ActivityCreateNavigateUpTaskStackHandler CreateNavigateUpTaskStack;
		event ActivityCreateOptionsMenuHandler CreateOptionsMenu;
		event ActivityCreatePanelMenuHandler CreatePanelMenu;
		event ActivityCreatePanelViewHandler CreatePanelView;
		event ActivityCreateThumbnailHandler CreateThumbnail;
		event ActivityCreateViewHandler CreateView;
		event ActivityCreateViewWithParentHandler CreateViewWithParent;
		event ActivityDestroyHandler Destroy;
		event ActivityDetachedFromWindowHandler DetachedFromWindow;
		event ActivityEnterAnimationCompleteHandler EnterAnimationComplete;
		event ActivityGenericMotionEventHandler GenericMotionEvent;
		event ActivityKeyDownHandler KeyDown;
		event ActivityKeyLongPressHandler KeyLongPress;
		event ActivityKeyMultipleHandler KeyMultiple;
		event ActivityKeyShortcutHandler KeyShortcut;
		event ActivityKeyUpHandler KeyUp;
		event ActivityLowMemoryHandler LowMemory;
		event ActivityMenuOpenedHandler MenuOpened;
		event ActivityNavigateUpHandler NavigateUp;
		event ActivityNavigateUpFromChildHandler NavigateUpFromChild;
		event ActivityNewIntentHandler NewIntent;
		event ActivityOptionsItemSelectedHandler OptionsItemSelected;
		event ActivityOptionsMenuClosedHandler OptionsMenuClosed;
		event ActivityPanelClosedHandler PanelClosed;
		event ActivityPauseHandler Pause;
		event ActivityPostCreateHandler PostCreate;
		event ActivityPostCreateWithPersistedStateHandler PostCreateWithPersistedState;
		event ActivityPostResumeHandler PostResume;
		event ActivityPrepareNavigateUpTaskStackHandler PrepareNavigateUpTaskStack;
		event ActivityPrepareOptionsMenuHandler PrepareOptionsMenu;
		event ActivityPrepareOptionsPanelHandler PrepareOptionsPanel;
		event ActivityPreparePanelHandler PreparePanel;
		event ActivityProvideAssistDataHandler ProvideAssistData;
		event ActivityRequestPermissionsResultWithResultsHandler RequestPermissionsResultWithResults;
		event ActivityRestartHandler Restart;
		event ActivityRestoreInstanceStateHandler RestoreInstanceState;
		event ActivityRestoreInstanceStateWithPersistedStateHandler RestoreInstanceStateWithPersistedState;
		event ActivityResumeHandler Resume;
		event ActivityResumeFragmentsHandler ResumeFragments;
		event ActivityRetainCustomNonConfigurationInstanceHandler RetainCustomNonConfigurationInstance;
		event ActivitySaveInstanceStateHandler SaveInstanceState;
		event ActivitySaveInstanceStateWithPersistedStateHandler SaveInstanceStateWithPersistedState;
		event ActivitySearchRequestedHandler SearchRequested;
		event ActivityStartHandler Start;
		event ActivityStateNotSavedHandler StateNotSaved;
		event ActivityStopHandler Stop;
		event ActivityTitleChangedHandler TitleChanged;
		event ActivityTopResumedActivityChangedHandler TopResumedActivityChanged;
		event ActivityTouchEventHandler TouchEvent;
		event ActivityTrackballEventHandler TrackballEvent;
		event ActivityTrimMemoryHandler TrimMemory;
		event ActivityUserInteractionHandler UserInteraction;
		event ActivityUserLeaveHintHandler UserLeaveHint;
		event ActivityVisibleBehindCanceledHandler VisibleBehindCanceled;
		event ActivityWindowAttributesChangedHandler WindowAttributesChanged;
		event ActivityWindowFocusChangedHandler WindowFocusChanged;
		event ActivityWindowStartingActionModeHandler WindowStartingActionMode;
	}
}
