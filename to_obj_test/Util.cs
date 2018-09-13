using System;
using Autodesk.Revit.DB;
using System.Diagnostics;

namespace to_obj_test
{
    class Util
    {

        public static string RealString(double a)//保留两位小数
        {
            return a.ToString("0.##");
        }

        public static string PluralSuffix(int n)//复数加s
        {
            return 1 == n ? "" : "s";
        }

        public static string ElementDescription(Element e)//Return a string describing the given element:
        {
            if (null == e)
            {
                return "<null>";
            }

            FamilyInstance fi = e as FamilyInstance;
            string typeName = e.GetType().Name;
            string categoryName = (null == e.Category) ? string.Empty : e.Category.Name + "";
            string familyName = (null == fi) ? string.Empty : fi.Symbol.Family.Name + " ";
            string symbolName = (null == fi || e.Name.Equals(fi.Symbol.Name)) ? string.Empty : fi.Symbol.Name + " ";
            return string.Format("{0} {1}{2}{3}<{4} {5}>", typeName, categoryName, familyName, symbolName, e.Id.IntegerValue, e.Name);
        }

        static int ColorToInt(Color color)
        {
            return color.Red << 16 | color.Green << 8 | color.Blue;
        }
        static Color IntToColor(int rgb)
        {
            return new Color((byte)((rgb & 0xFF0000) >> 16), (byte)((rgb & 0xFF00) >> 8), (byte)(rgb & 0xFF));
        }

        public static int ColorTransparencyToInt(Color color, int transparency)
        {
            Debug.Assert(0 <= transparency,"expected non-negative transparency");
            Debug.Assert(100 >= transparency,"expected transparency between 0 and 100");
            uint trgb = ((uint)transparency << 24) | (uint)ColorToInt(color);
            Debug.Assert(int.MaxValue > trgb, "expected trgb smaller than max int");
            return (int)trgb;
        }
        public static Color IntToColorTransparency(int trgb, out int transparency)
        {
            transparency = (int)((((uint)trgb) & 0xFF000000) >> 24);
            return IntToColor(trgb);
        }

        static string ColorString(Color color)//指定X2，16进制显示的更整齐
        {
            return color.Red.ToString("X2") + color.Green.ToString("X2") + color.Blue.ToString("X2");
        }
        public static string ColorTransparencyString( Color color, int transparency)
        {
            return transparency.ToString("X2") + ColorString(color);
        }

        int GetRevitTextColorFromSystemColor( System.Drawing.Color color)
        {
            return (((int)color.R) * (int)Math.Pow(2, 0)
              + ((int)color.G) * (int)Math.Pow(2, 8)
              + ((int)color.B) * (int)Math.Pow(2, 16));
        }
        void RevitTextColorFromSystemColorUsageExample()
        {
            TextNoteType tnt = null;

            // Set Revit text colour from system colour

            int color = GetRevitTextColorFromSystemColor( System.Drawing.Color.Wheat);
            tnt.get_Parameter(BuiltInParameter.LINE_COLOR) .Set(color);
        }
    }
}
