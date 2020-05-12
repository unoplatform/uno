#if __ANDROID__

using System.Xml;

namespace Windows.UI.Notifications
{
    [Android.Content.BroadcastReceiver()]  // Name = "UnoLiveTileEmulator"
    [Android.App.IntentFilter(new[] { Android.Appwidget.AppWidgetManager.ActionAppwidgetUpdate , Android.Appwidget.AppWidgetManager.ActionAppwidgetOptionsChanged  })]
    [Android.App.MetaData(Android.Appwidget.AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/livetileinfo") ]
    public class AndroidBroadcastReceiverForUWPLiveTile : Android.Appwidget.AppWidgetProvider
    {

        private static string size2templateName(Android.OS.Bundle options)
        { // get Live Tile template name (small|medium|wide|large) from current widget size
            int minW = options.GetInt(Android.Appwidget.AppWidgetManager.OptionAppwidgetMinWidth);
            int maxW = options.GetInt(Android.Appwidget.AppWidgetManager.OptionAppwidgetMaxWidth);
            int minH = options.GetInt(Android.Appwidget.AppWidgetManager.OptionAppwidgetMinHeight);
            int maxH = options.GetInt(Android.Appwidget.AppWidgetManager.OptionAppwidgetMaxHeight);

            int tileWidth = minW;   // should be == maxW
            int tileHeight = minH;  // should be == maxH
            if ((minW != maxW))
            {
                // shouldn't occur, but use average
                tileWidth = (minW + maxW) / 2;
            }

            if ((minH != maxH))
            {
                // shouldn't occur, but use average
                tileHeight = (minH + maxH) / 2;
            }

            // maybe exchange tileHeight/tileWidth when screen flipped, but at least SquareHome doesn't rotate 

            // maybe conversion between "display points" and "logical points" (using DPI)

            // There are 4 tile sizes: small (71 x 71), medium (150 x 150), wide (310 x 150), and large (310 x 310)

            if (tileHeight < 140)
                return "TileSmall";

            if (tileHeight < 300)
                return "TileMedium";

            if (tileWidth < 300)
                return "TileWide";

            return "TileLarge";
        }

        private static XmlNode EmptyTileTemplate()
        {// default template 
            var xmlLT = new XmlDocument();
            string sXml = "<tile><visual>";
            sXml = sXml + "<binding template ='TileMedium' branding='none'>";
            sXml = sXml + "<text hint-style='caption'>" + Android.App.Application.Context.PackageName + "</text>";
            sXml = sXml + "</binding>";
            sXml = sXml + "</visual></tile>";

            xmlLT.LoadXml(sXml);

            var template = xmlLT.SelectSingleNode("//binding");
            return template;
        }

        private static XmlNode GetLTTemplate(Android.OS.Bundle options)
        {
            string liveTile = TileUpdater._TileTemplate;
            if(string.IsNullOrEmpty(liveTile))
                return EmptyTileTemplate();

            var xmlLT = new XmlDocument();
            xmlLT.LoadXml(liveTile);

            string templateName = size2templateName(options);
            // <tile><visual><binding template ='TileWide' branding='none'>";'

            // return this, or smaller template
            do
            {
                var template = xmlLT.SelectSingleNode("//binding[@template='" + templateName + "']");
                if (template != null)
                    return template;

                // we don't have template for this size, so try smaller template
                switch (templateName)
                {
                    case "TileMedium":
                        templateName = "TileSmall";
                        break;
                    case "TileWide":
                        templateName = "TileMedium";
                        break;
                    case "TileLarge":
                        templateName = "TileWide";
                        break;
                    default:
                        templateName = "";
                        break;
                }

            } while (templateName != "");

            // we don't have requested template, nor smaller template - try to get bigger template
            templateName = size2templateName(options);
            do
            {
                var template = xmlLT.SelectSingleNode("//binding[@template='" + templateName + "']");
                if (template != null)
                    return template;

                switch (templateName)
                {
                    case "TileSmall":
                        templateName = "TileMedium";
                        break;
                    case "TileMedium":
                        templateName = "TileWide";
                        break;
                    case "TileWide":
                        templateName = "TileLarge";
                        break;
                    default:
                        templateName = "";
                        break;
                }


            } while (templateName != "");


            // cannot find ANY template
            // maybe widget is added, before call to Windows.UI.Notifications.TileUpdateManager.CreateTileUpdaterForApplication().Update()
            return EmptyTileTemplate();

        }

        private static int UwpHintStyle2Points(string hintStyle)
        {
            if (string.IsNullOrEmpty(hintStyle))
                return 15;

            hintStyle = hintStyle.Replace("Numeral", "");   // smaller line height
            hintStyle = hintStyle.Replace("Subtle", "");    // opacity 60 %, *TODO* jako color=0x99rrggbb

            switch (hintStyle.ToLower())
            {
                case "caption":
                    return 12;
                case "body":
                    return 15;
                case "base":
                    return 15;
                case "subtitle":
                    return 20;
                case "title":
                    return 24;
                case "subheader":
                    return 34;
                case "header":
                    return 46;
            }
            return 15;

            // caption   12 regular
            // body      15 regular
            // base      15 semibold
            // subtitle  20 regular
            // title     24 semilight
            // subheader 34 light
            // header    46 light
        }

        private static int UwpHintAlign2Gravity(string hintAlign)
        {
            if (string.IsNullOrEmpty(hintAlign))
                return (int)Android.Views.GravityFlags.Left;

            switch (hintAlign.ToLower())
            {
                case "left":
                    return (int)Android.Views.GravityFlags.Left;
                case "center":
                    return (int)Android.Views.GravityFlags.Center;
                case "right":
                    return (int)Android.Views.GravityFlags.Right;
            }
            return (int)Android.Views.GravityFlags.Left;
        }


        internal static void updateWidget(Android.Content.Context context, Android.Appwidget.AppWidgetManager appWidgetManager, int appWidgetId, Android.OS.Bundle options)
        {
            var liveTileTemplate = GetLTTemplate(options);
            if (liveTileTemplate is null)
                return;

            int layoutId = TileUpdater._IdTileLayout;

            if (layoutId == 0 )
			{ // without 'global' prefix, System is not _this_ system, and there are no NullReferenceException inside it
				throw new global::System.NullReferenceException("LiveTile: missing Ids, check if TileUpdater.AndroidInitIds was called from app");
            }
            
            var views = new Android.Widget.RemoteViews(context.PackageName, layoutId);

            int txtLine;

            // iterating texts from template
            var txtNodes = liveTileTemplate.SelectNodes("text");
            for (txtLine = 0; txtLine < txtNodes.Count; txtLine++)
            {
                var txtNode = txtNodes[txtLine];
                XmlAttributeCollection txtAttribs = txtNode.Attributes;

                views.SetTextViewText(TileUpdater._IdTxtLines[txtLine], txtNode.InnerText);

                views.SetViewVisibility(TileUpdater._IdTxtLines[txtLine], Android.Views.ViewStates.Visible);

                views.SetTextViewTextSize(TileUpdater._IdTxtLines[txtLine], (int)Android.Util.ComplexUnitType.Pt,
                    UwpHintStyle2Points(txtAttribs.GetNamedItem("hint-style")?.InnerText));

                // hint-align? = "left" | "center" | "right"
                views.SetInt(TileUpdater._IdTxtLines[txtLine], "setGravity",
                    UwpHintAlign2Gravity(txtAttribs.GetNamedItem("hint-align")?.InnerText));

                string hint;

                // hint-wrap? = boolean
                hint = txtAttribs.GetNamedItem("hint-wrap")?.InnerText;
                if (hint is null)
                {
                    hint = "false";
                }
                views.SetBoolean(TileUpdater._IdTxtLines[txtLine], "setSingleLine", (hint.ToLower() == "false"));

                // line sizes
                int lineCount = 1;

                // hint-maxLines? = integer setMaxLines(int)
                hint = txtAttribs.GetNamedItem("hint-maxLines")?.InnerText;
                if (hint is null)
                {
                    hint = "1";
                }
                if (!int.TryParse(hint, out lineCount))
                {
                    lineCount = 1;
                }
                views.SetInt(TileUpdater._IdTxtLines[txtLine], "setMaxLines", lineCount);


                // hint-minLines? = integer setMinLines(int)
                hint = txtAttribs.GetNamedItem("hint-minLines")?.InnerText;
                if (hint is null)
                {
                    hint = "1";
                }
                if (!int.TryParse(hint, out lineCount))
                {
                    lineCount = 1;
                }
                views.SetInt(TileUpdater._IdTxtLines[txtLine], "setMinLines", lineCount);

            }
            // and hide all next lines
            for (; txtLine < 10; txtLine++)
            {
                views.SetViewVisibility(TileUpdater._IdTxtLines[txtLine], Android.Views.ViewStates.Gone);
            }

            // here our widget is redrawn with "Couldn't add widget" text!
			// even when all code between getting views (line 204) and this line is commented out
            appWidgetManager.UpdateAppWidget(appWidgetId, views);


        }

        public override void OnAppWidgetOptionsChanged(Android.Content.Context context, Android.Appwidget.AppWidgetManager appWidgetManager, int appWidgetId, Android.OS.Bundle newOptions)
        {
                updateWidget(context, appWidgetManager, appWidgetId, newOptions);
        }

        internal static void UpdateAllIds(Android.Content.Context context, Android.Appwidget.AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            if (appWidgetIds is null || appWidgetIds.Length < 1)
                return;

            foreach(int appWidgetId in appWidgetIds)
            {
                Android.OS.Bundle opcje = appWidgetManager.GetAppWidgetOptions(appWidgetId);    // since API 16
                //updateWidget(context, appWidgetManager, appWidgetId, opcje);
            }

        }

        public override void OnUpdate(Android.Content.Context context, Android.Appwidget.AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            UpdateAllIds(context, appWidgetManager, appWidgetIds);
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }


    }


}

#endif
