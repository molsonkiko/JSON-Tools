using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JSON_Viewer.Forms
{
    public partial class TreeViewer : Form
    {
        public TreeViewer()
        {
            InitializeComponent();
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            textBox1.Text = "You have been helped!";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
