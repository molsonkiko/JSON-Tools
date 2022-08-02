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

        static void Main()
        {
            Application.Run(new TreeViewer());
            //YamlDumper.RunAll(new string[] {});
        }

        public TreeViewer()
        {
            InitializeComponent();
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
            JsonParser parser = new JsonParser();
            JNode json = parser.Parse(JsonBox.Text);
            JsonTreePopulate(json);
        }

        private void JsonTreePopulate(JNode json)
        {
            JsonTree.BeginUpdate();
            JsonTree.Nodes.Clear();
            TreeNode root = new TreeNode();
            JsonTreePopulateHelper(root, json);
            JsonTree.Nodes.Add(root);
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
