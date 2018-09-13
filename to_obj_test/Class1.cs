using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Application = Autodesk.Revit.ApplicationServices.Application;
using Autodesk.Revit.Utility;
using System.Collections.Generic;

namespace to_obj_test
{
    //显示一个非模态窗体
    //模态窗体不允许操作其他窗体，非模态窗体可以操作其他窗体。
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class cmdShowForm : IExternalCommand
    {
        

        public Result Execute(ExternalCommandData cmdData, ref string msg, ElementSet elements)
        {
            form frmCmd = new form(cmdData, msg, elements);
            frmCmd.Show();
            return Result.Succeeded;
        }
    }


    [TransactionAttribute(TransactionMode.Manual)]
    public class Class1 : IExternalCommand
    {
        public static Color DefaultColor = new Color(127, 127, 127);    //默认灰色
        //static string _export_folder_name = null;
        string TextPath = string.Empty;
        string TextName = string.Empty;
        const string BitmapPropertyName = "unifiedbitmap_Bitmap";

        public void getTextPath(string path)
        {
            TextPath = path;
        }

        public void getTextName(string names)
        {
            TextName = names;
        }

        void InfoMsg(string msg)
        {
            TaskDialog.Show("OBJ Exporter",msg);
        }

        //static bool FileSelect(string folder, out string filename)
        //{
        //    SaveFileDialog dlg = new SaveFileDialog();    //保存文件对话框
        //    dlg.Title = "Select OBJ Output File";
        //    dlg.CheckFileExists = false;
        //    dlg.CheckPathExists = true;
        //    //dlg.RestoreDirectory = true;
        //    dlg.InitialDirectory = "D:\\to_obj";
        //    dlg.Filter = "OBJ Files (*.obj)|*.obj|All Files (*.*)|*.*";
        //    bool rc = (DialogResult.OK == dlg.ShowDialog());     //dialogresult.ok表示点击确定
        //    filename = dlg.FileName;
        //    return rc;
        //}

        //导出一个非空solid.
        bool ExportSolid(
            IJtFaceEmitter emitter,
            Document doc,
            Solid solid,
            Color color,
            int shininess,
            int transparency)
        {
            Material m;
            Color c;
            int s, t;

            foreach(Face face in solid.Faces)
            {
                m = doc.GetElement(face.MaterialElementId) as Material;
                c = (null == m) ? color : m.Color;
                s = (null == m) ? shininess : m.Shininess;
                t = (null == m) ? transparency : m.Transparency;

                emitter.EmitFace(face,(null == c) ? DefaultColor : c, s, t);
            }

            return true;
        }

        int ExportSolids(
            IJtFaceEmitter emitter,
            Element e,
            Options opt,
            Color color,
            int shininess,
            int transparency)
        {
            int nSolids = 0;
            GeometryElement geo = e.get_Geometry(opt);

            Solid solid;

            if (null != geo)
            {
                Document doc = e.Document;

                if (e is FamilyInstance)
                {
                    geo = geo.GetTransformed(Transform.Identity);
                }

                GeometryInstance inst = null;
                foreach (GeometryObject obj in geo)
                {
                    solid = obj as Solid;

                    if (null != solid && 0 < solid.Faces.Size && ExportSolid(emitter, doc, solid,color, shininess, transparency))
                    {
                        ++nSolids;
                    }

                    inst = obj as GeometryInstance;
                }

                if (0 == nSolids && null != inst)
                {
                    geo = inst.GetSymbolGeometry();

                    foreach (GeometryObject obj in geo)
                    {
                        solid = obj as Solid;

                        if (null != solid && 0 < solid.Faces.Size && ExportSolid(emitter, doc, solid,color, shininess, transparency))
                        {
                            ++nSolids;
                        }
                    }
                }
            }
            return nSolids;
        }

        int ExportElement(
            IJtFaceEmitter emitter,
            Element e,
            Options opt,
            ref int nSolids)
        {
            Group group = e as Group;
            if (null != group)
            {
                int n = 0;
                foreach (ElementId id in group.GetMemberIds())
                {
                    Element e2 = e.Document.GetElement(id);
                    n += ExportElement(emitter, e2, opt, ref nSolids);
                }
                return n;
            }

            string desc = Util.ElementDescription(e);

            Category cat = e.Category;
            if (null == cat)
            {
                Debug.Print("Element '{0}' has no " + "category.", desc);
                return 0;
            }

            Material material = cat.Material;
            Color color = (null == material) ? null : material.Color;
            int transparency = (null == material) ? 0 : material.Transparency;
            int shininess = (null == material) ? 0 : material.Shininess;

            nSolids += ExportSolids(emitter, e, opt, color, shininess, transparency);

            return 1;
        }

        void ExportElements(
            IJtFaceEmitter emitter,
            FilteredElementCollector collector,
            Options opt)
        {
            int nElements = 0;
            int nSolids = 0;

            foreach (Element e in collector)
            {
                nElements += ExportElement(
                  emitter, e, opt, ref nSolids);
            }

            int nFaces = emitter.GetFaceCount();
            int nTriangles = emitter.GetTriangleCount();
            int nVertices = emitter.GetVertexCount();

            string msg = string.Format(
                "{0} element{1} with {2} solid{3}, "
                 + "{4} face{5}, {6} triangle{7} and "
                + "{8} vertice{9} exported.",
                nElements, Util.PluralSuffix(nElements),
                nSolids, Util.PluralSuffix(nSolids),
                nFaces, Util.PluralSuffix(nFaces),
                nTriangles, Util.PluralSuffix(nTriangles),
                nVertices, Util.PluralSuffix(nVertices));

            InfoMsg(msg);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            
            form fm = new form();
            Document doc = app.OpenDocumentFile(TextPath);

            //决定导出的元素
            FilteredElementCollector collector = null;
   
            collector = new FilteredElementCollector(doc);

            collector.WhereElementIsNotElementType().WhereElementIsViewIndependent();
 
            ObjExporter exporter = new ObjExporter();
            Options opt = app.Create.NewGeometryOptions();
            ExportElements(exporter, collector, opt);

            

            Directory.CreateDirectory(@"D:\to_obj\" + TextName.ToString());
            exporter.ExportTo(@"D:\to_obj\"+TextName.ToString()+ @"\"+TextName.ToString()+".obj".ToString());

            Directory.CreateDirectory(@"D:\to_obj\" + TextName.ToString() + @"\" + "Texture");
            //var objlibraryAsset = app.get_Assets(AssetType.Appearance);
            //foreach (Element elem in collector)
            //{
            //    string theValue = null;
            //    ICollection<ElementId> ids = elem.GetMaterialIds(false);
            //    foreach (ElementId id in ids)
            //    {
            //        Material mat = doc.GetElement(id) as Material;
            //        AssetPropertyString bitmapProperty = null;
            //        AssetProperty property;

            //        ElementId appearanceId = mat.AppearanceAssetId;
            //        AppearanceAssetElement appearanceElem = doc.GetElement(appearanceId) as AppearanceAssetElement;
            //        Asset asset = appearanceElem.GetRenderingAsset();
            //        if (0 != asset.Size)
            //        {
            //            for (var j = 0; j < asset.Size; j++)
            //            {
            //                property = asset[j];
            //                if (property.Name == BitmapPropertyName)
            //                {
            //                    bitmapProperty = property as AssetPropertyString;
            //                    if (bitmapProperty != null)
            //                    {
            //                        theValue = bitmapProperty.Value;
            //                        break;
            //                    }
            //                }
            //            }
            //        }
            //        else
            //        {
            //            foreach (Asset objCurrentAsset in objlibraryAsset)
            //            {
            //                if (objCurrentAsset.Name == asset.Name &&
            //                    objCurrentAsset.LibraryName == asset.LibraryName)
            //                {
            //                    theValue = objCurrentAsset.Type.ToString();
            //                }
            //            }
            //        }

            //    }
            //    if (theValue != null)
            //    {
            //        TaskDialog.Show("lalala", theValue);
            //    }
                

            //}
            return Result.Succeeded;
        }
    }
}
