using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fcHMI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public  bool bStop { get; set; }

        /// <summary>
        /// Helper method to determin if invoke required, if so will rerun method on correct thread.
        /// if not do nothing.
        /// </summary>
        /// <param name="c">Control that might require invoking</param>
        /// <param name="a">action to preform on control thread if so.</param>
        /// <returns>true if invoke required</returns>
        public bool ControlInvokeRequired(Control c, Action a)
        {
            if (c.InvokeRequired) c.Invoke(new MethodInvoker(delegate { a(); }));
            else return false;

            return true;
        }
        public void SetLog(string sMsg)
        {
            if (ControlInvokeRequired(txtConsole, () => SetLog(sMsg))) return;
            
            txtConsole.Text = sMsg;
            
        }

        private void btRead_Click(object sender, EventArgs e)
        {
            //Program.sMessage = "fero";
            SetLog(Program.sMessage);
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            bStop = true;
        }
    }
}
