#!/usr/bin/env bash

echo ""
echo "[Waiting for device to boot]"

if [ $ANDROID_SIMULATOR_APILEVEL -gt 25 ];
then 
$ANDROID_HOME/platform-tools/adb wait-for-device shell 'while [[ -z $(getprop sys.boot_completed | tr -d '\r') ]]; do sleep 1; done; input keyevent 82'
else
$ANDROID_HOME/platform-tools/adb wait-for-device shell 'while [[ -z $(getprop sys.boot_completed) ]]; do sleep 1; done; input keyevent 82'
fi

# Wait for com.android.systemui to become available,
# as the CPU of the build machine may be slow
# See: https://stackoverflow.com/questions/52410440/error-system-ui-isnt-responding-while-running-aosp-build-on-emulator
#

echo ""
echo "[Waiting for launcher to start]"
LAUNCHER_READY=
START_TIME=$SECONDS
MAX_START_TIME=300
while [[ -z ${LAUNCHER_READY} ]]; do

    if [ $ANDROID_SIMULATOR_APILEVEL -ge 29 ];
    then 
    UI_FOCUS=`$ANDROID_HOME/platform-tools/adb shell dumpsys window 2>/dev/null | grep -E 'mCurrentFocus|mFocusedApp'`
    else
    UI_FOCUS=`$ANDROID_HOME/platform-tools/adb shell dumpsys window windows 2>/dev/null | grep -i mCurrentFocus`
    fi

    ELAPSED_TIME=$(( SECONDS - START_TIME ))
    if [ ${ELAPSED_TIME} -gt ${MAX_START_TIME} ];
    then
        echo "(FAIL) Emulator failed to start properly after $MAX_START_TIME"
        exit 1
    fi

    echo "(DEBUG) Current focus: ${UI_FOCUS}"

    case $UI_FOCUS in
    *"Launcher"*)
        LAUNCHER_READY=true
    ;;
    "")
        echo "Waiting for window service..."
        sleep 3
    ;;
    *"Not Responding"*)
        echo "Detected an ANR! Dismissing..."
        $ANDROID_HOME/platform-tools/adb shell input keyevent KEYCODE_DPAD_DOWN
        $ANDROID_HOME/platform-tools/adb shell input keyevent KEYCODE_DPAD_DOWN
        $ANDROID_HOME/platform-tools/adb shell input keyevent KEYCODE_ENTER
    ;;
    *)
        echo "Waiting for launcher..."
        sleep 3

		# For some reason the messaging app can be brought up in front
		# (DEBUG) Current focus:   mCurrentFocus=Window{1170051 u0 com.google.android.apps.messaging/com.google.android.apps.messaging.ui.ConversationListActivity}
		# Try bringing back the home screen to check on the launcher.
        $ANDROID_HOME/platform-tools/adb shell input keyevent KEYCODE_HOME
    ;;
    esac
done

echo "Launcher is ready!"
