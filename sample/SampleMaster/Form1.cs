using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SampleMaster
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public void SetLog(string sMsg)
        {
            txtConsole.Text = sMsg;
        }

        private void btRead_Click(object sender, EventArgs e)
        {
            //Program.sMessage = "fero";
            SetLog(Program.sMessage);
        }
    }
}
