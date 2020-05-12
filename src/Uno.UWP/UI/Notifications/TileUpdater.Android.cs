#if __ANDROID__

namespace Windows.UI.Notifications
{
    public partial class TileUpdater
    {
        private string _tileName;  // null: primary tile; not empty: name of secondary tile
        static internal int _IdTileLayout = 0;  // per Tile..
        static internal int[] _IdTxtLines = new int[10]; // per Tile...
        static internal string _TileTemplate = "";

        public static void AndroidInitIds(int layout, int line0, int line1, int line2, int line3, int line4, int line5, int line6, int line7, int line8, int line9)
        {
            // this must be called from Android head before using LiveTile 
            _IdTileLayout = layout;

            // IDs should be consecutive, but...
            _IdTxtLines[0] = line0;
            _IdTxtLines[1] = line1;
            _IdTxtLines[2] = line2;
            _IdTxtLines[3] = line3;
            _IdTxtLines[4] = line4;
            _IdTxtLines[5] = line5;
            _IdTxtLines[6] = line6;
            _IdTxtLines[7] = line7;
            _IdTxtLines[8] = line8;
            _IdTxtLines[9] = line9;
        }

        internal TileUpdater(string tileName)
        {
            _tileName = tileName;
        }

        public void Update(TileNotification notification)
        {
            var context = Android.App.Application.Context;
            var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
            var component = new Android.Content.ComponentName(context,
                Java.Lang.Class.FromType(typeof(AndroidBroadcastReceiverForUWPLiveTile)).Name);

            int[] views = appWidgetManager.GetAppWidgetIds(component);
            AndroidBroadcastReceiverForUWPLiveTile.UpdateAllIds(context, appWidgetManager, views);
            //appWidgetManager.NotifyAppWidgetViewDataChanged(views, )

            //for(int viewNo = 0; viewNo < views.GetLength(0); viewNo++)
            //{
            //    var remViews = new Android.Widget.RemoteViews(context.PackageName, viewNo);
            //    appWidgetManager.UpdateAppWidget(component, remViews);
            //}
        }
    }


}

#endif
