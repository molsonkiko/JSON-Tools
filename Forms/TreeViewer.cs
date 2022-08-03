/*
A form for creating a tree view of JSON and also making Remespath queries.
*/
using JSON_Viewer.JSONViewerNppPlugin;

namespace JSON_Viewer.Forms
{
    public partial class TreeViewer : Form
    {
        public JsonParser jsonParser { get; set; }
        public RemesParser remesParser { get; set; }
        public JNode json { get; set; }

        [STAThread] // this is needed to allow your form to open up a file browser dialog while in debug mode
        static void Main(string[] args)
        {
            Application.Run(new TreeViewer());
            Runner.RunAll(args); // run all tests for the app
        }

        public TreeViewer()
        {
            InitializeComponent();
            jsonParser = new JsonParser();
            remesParser = new RemesParser();
        }

        private static readonly string HELP_TEXT = "You have been helped!";

        private void HelpButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(HELP_TEXT,
                    "JSON Tree Viewer Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
        }

        //private void textBox1_TextChanged(object sender, EventArgs e)
        //{

        //}

        private void TreeCreationButton_MouseUp(object sender, EventArgs e)
        {
            JsonTreePopulate(JsonBox.Text);
        }

        private void FileSelectionButton_MouseUp(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "JSON files|*.json|All files|*.*|Jupyter Notebooks|*.ipynb";
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Title = "Browse JSON files";
            openFileDialog1.CheckFileExists = true;
            string json_str = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (!File.Exists(openFileDialog1.FileName))
                {
                    return;
                }
                json_str = File.ReadAllText(openFileDialog1.FileName);
            }
            JsonTreePopulate(json_str);
        }

        private void JsonTreePopulate(string json_str)
        {
            json = new JNode(null, Dtype.NULL, 0);
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
            root.Text = "JSON";
            root.Expand();
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

        private void QueryBox_PressEnter(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                JNode query_result = remesParser.Search(QueryBox.Text, json);
                JsonBox.Text = query_result.PrettyPrint();
            }
        }
    }
}
