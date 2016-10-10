using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Autodesk.Revit.UI;

namespace ConvertDWGtoLines
{
    public partial class ProgressForm : Form
    {
        public ProgressForm(UIApplication uiapp)
        {
            InitializeComponent();
        }

        public void SetupProgress(int max, string info)
        {
            MethodInvoker mi = delegate
            {
                progressBar1.Minimum = 0;
                progressBar1.Maximum = max;
                progressBar1.Value = 0;
                progressBar1.Visible = true;
                txtLbl1.Text = info;
                //pictureBox1.Visible = false;
                this.Refresh();
                //System.Windows.Forms.Application.DoEvents();
            };
            if (InvokeRequired)
            {
                this.Invoke(mi);
            }
            else
            {
                progressBar1.Minimum = 0;
                progressBar1.Maximum = max;
                progressBar1.Value = 0;
                progressBar1.Visible = true;
                txtLbl1.Text = info;
                //pictureBox1.Visible = false;
                this.Refresh();
                //System.Windows.Forms.Application.DoEvents();
            }
        }

        public void IncrementProgress()
        {
            MethodInvoker mi = delegate
            {
                ++progressBar1.Value;
                this.Refresh();
            };
            if (InvokeRequired)
            {
                this.Invoke(mi);
            }
            else
            {
                ++progressBar1.Value;
                this.Refresh();
            }
            System.Windows.Forms.Application.DoEvents();
        }

        public void EndOfConversion(String stringToShow)
        {
            txtLbl1.Text = stringToShow;
            btnCancel.Visible = true;
            progressBar1.Visible = false;

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
