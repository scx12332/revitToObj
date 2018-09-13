using Autodesk.Revit.UI;
using System;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using to_obj_test;
using System.IO;

namespace to_obj_test
{
    public partial class form : System.Windows.Forms.Form
    {
        public string text;
        public string name;
        ExternalCommandData cmdDataForm;
        ElementSet elementsForm = new ElementSet();
        string msgForm;
        //static string _export_folder_name = null;

        public form()
        {
            InitializeComponent();
        }

        public form(ExternalCommandData cmdData, string msg, ElementSet elements)
        {
            InitializeComponent();
            cmdDataForm = cmdData;
            msgForm = msg;
            elementsForm = elements;
        }


        static bool FileSelect(string folder, out string filename)
        {
            SaveFileDialog dlg = new SaveFileDialog();    //保存文件对话框
            dlg.Title = "Select OBJ Output File";
            dlg.CheckFileExists = false;
            dlg.CheckPathExists = true;
            //dlg.RestoreDirectory = true;
            dlg.InitialDirectory = "D:\\to_obj";
            dlg.Filter = "OBJ Files (*.obj)|*.obj|All Files (*.*)|*.*";
            bool rc = (DialogResult.OK == dlg.ShowDialog());     //dialogresult.ok表示点击确定
            filename = dlg.FileName;
            return rc;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            string valu = "";

            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*rvt*)|*.rvt;*.rfa"; //设置要选择的文件的类型
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {

                //if (null == _export_folder_name)
                //{
                //    _export_folder_name = Path.GetTempPath();
                //}

                //string filename = null;
                //打开文件选择
                //if (!FileSelect(_export_folder_name, out filename))
                //{
                //    return Result.Cancelled;
                //}
                //_export_folder_name = Path.GetDirectoryName(filename);

                TaskDialog.Show("info", "开始导入");
                for (int i = 0; i < fileDialog.SafeFileNames.Length; i++)
                {
                    string file = fileDialog.FileNames.GetValue(i).ToString();//返回文件的完整路径 
                    valu = fileDialog.SafeFileNames.GetValue(i).ToString();
                    textBox1.Text += valu;//File.ReadAllText(file, Encoding.GetEncoding("utf-8"));
                    text = file;
                    name = valu.Substring(0, valu.LastIndexOf("."));

                    Class1 cl1 = new Class1();
                    cl1.getTextPath(text);
                    cl1.getTextName(name);
                    cl1.Execute(cmdDataForm, ref msgForm, elementsForm);
                }
                TaskDialog.Show("Succeeded!!", "Succeeded!");

            }
        }
    }
}
