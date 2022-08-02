using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JSON_Viewer.JSONViewerNppPlugin;

namespace JSON_Viewer.Forms
{
    public partial class TreeViewer : Form
    {
        public JsonParser jsonParser { get; set; }

        [STAThread] // this is needed to allow your form to open up a file browser dialog while in debug mode
        static void Main()
        {
            Application.Run(new TreeViewer());
            //YamlDumper.RunAll(new string[] {});
        }

        public TreeViewer()
        {
            InitializeComponent();
            jsonParser = new JsonParser();
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            JsonBox.Text = "You have been helped!";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void TreeCreationButton_MouseUp(object sender, EventArgs e)
        {
            JsonTreePopulate(JsonBox.Text);
        }

        private void FileSelectionButton_MouseUp(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "All files|*.*|JSON files|*.json|Jupyter Notebooks|*.ipynb";
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Title = "Browse JSON files";
            openFileDialog1.CheckFileExists = true;
            string json_str = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                json_str = File.ReadAllText(openFileDialog1.FileName);
            }
            JsonTreePopulate(json_str);
        }

        private void JsonTreePopulate(string json_str)
        {
            JNode json = new JNode(null, Dtype.NULL, 0);
            try
            {
                json = jsonParser.Parse(json_str);
            }
            catch (JsonParserException ex)
            {
                string error_text = $"JSON parsing failed:\n{ex.ToString()}";
                MessageBox.Show(error_text, 
                    "JSON parsing error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                return;
            }
            JsonTree.BeginUpdate();
            UseWaitCursor = true; // get the spinny cursor that means the computer is processing
            JsonTree.Nodes.Clear();
            TreeNode root = new TreeNode();
            JsonTreePopulateHelper(root, json);
            JsonTree.Nodes.Add(root);
            UseWaitCursor = false; // gotta turn it off or else it persists until the form closes
            JsonTree.EndUpdate();
        }
        
        private void JsonTreePopulateHelper(TreeNode root, JNode json)
        {
            if (json is JArray)
            {
                List<JNode> jar = ((JArray)json).children;
                for (int ii = 0; ii < jar.Count; ii++)
                {
                    JNode child = jar[ii];
                    if (child.type == Dtype.ARR || child.type == Dtype.OBJ)
                    {
                        // it's an array or object, so add a subtree
                        var child_node = new TreeNode(ii.ToString());
                        JsonTreePopulateHelper(child_node, child);
                        root.Nodes.Add(child_node);
                    }
                    else
                    {
                        // it's a scalar, so just display the index and the value of the json
                        root.Nodes.Add(ii.ToString(), $"{ii} : {child.ToString()}");
                    }
                }
                return;
            }
            // create a subtree for a json object
            // scalars are dealt with already, so no need for a separate branch
            Dictionary<string, JNode> dic = ((JObject)json).children;
            foreach ((string key, JNode child) in dic)
            {
                if (child.type == Dtype.ARR || child.type == Dtype.OBJ)
                {
                    // it's an array or object, so add a subtree
                    var child_node = new TreeNode(key);
                    JsonTreePopulateHelper(child_node, child);
                    root.Nodes.Add(child_node);
                }
                else
                {
                    // it's a scalar, so just display the key and the value of the json
                    root.Nodes.Add(key, $"{key} : {child.ToString()}");
                }
            }
        }
    }
}
