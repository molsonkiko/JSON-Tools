namespace JSON_Viewer.Forms
{
    partial class TreeViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TreeViewer));
            this.HelpButton = new System.Windows.Forms.Button();
            this.JsonBox = new System.Windows.Forms.TextBox();
            this.JsonTree = new System.Windows.Forms.TreeView();
            this.TreeCreationButton = new System.Windows.Forms.Button();
            this.FileSelectionButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // HelpButton
            // 
            this.HelpButton.Location = new System.Drawing.Point(332, 50);
            this.HelpButton.Name = "HelpButton";
            this.HelpButton.Size = new System.Drawing.Size(62, 29);
            this.HelpButton.TabIndex = 0;
            this.HelpButton.Text = "Help";
            this.HelpButton.UseVisualStyleBackColor = true;
            this.HelpButton.Click += new System.EventHandler(this.HelpButton_Click);
            // 
            // JsonBox
            // 
            this.JsonBox.Location = new System.Drawing.Point(424, 131);
            this.JsonBox.Multiline = true;
            this.JsonBox.Name = "JsonBox";
            this.JsonBox.Size = new System.Drawing.Size(212, 108);
            this.JsonBox.TabIndex = 1;
            this.JsonBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // JsonTree
            // 
            this.JsonTree.Location = new System.Drawing.Point(51, 84);
            this.JsonTree.Name = "JsonTree";
            this.JsonTree.Size = new System.Drawing.Size(204, 311);
            this.JsonTree.TabIndex = 2;
            // 
            // TreeCreationButton
            // 
            this.TreeCreationButton.Location = new System.Drawing.Point(424, 258);
            this.TreeCreationButton.Name = "TreeCreationButton";
            this.TreeCreationButton.Size = new System.Drawing.Size(132, 29);
            this.TreeCreationButton.TabIndex = 3;
            this.TreeCreationButton.Text = "Create JSON tree";
            this.TreeCreationButton.UseVisualStyleBackColor = true;
            this.TreeCreationButton.Click += new System.EventHandler(this.TreeCreationButton_MouseUp);
            // 
            // FileSelectionButton
            // 
            this.FileSelectionButton.Location = new System.Drawing.Point(441, 349);
            this.FileSelectionButton.Name = "FileSelectionButton";
            this.FileSelectionButton.Size = new System.Drawing.Size(152, 29);
            this.FileSelectionButton.TabIndex = 4;
            this.FileSelectionButton.Text = "Open JSON file";
            this.FileSelectionButton.UseVisualStyleBackColor = true;
            this.FileSelectionButton.Click += new System.EventHandler(this.FileSelectionButton_MouseUp);
            // 
            // TreeViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.FileSelectionButton);
            this.Controls.Add(this.TreeCreationButton);
            this.Controls.Add(this.JsonTree);
            this.Controls.Add(this.JsonBox);
            this.Controls.Add(this.HelpButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TreeViewer";
            this.Text = "JSON Tree Viewer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button HelpButton;
        private TextBox JsonBox;
        private TreeView JsonTree;
        private Button TreeCreationButton;
        private Button FileSelectionButton;
    }
}