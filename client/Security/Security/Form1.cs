using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Security
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Security.Program.sec>=0)
            {
                Security.Program.sec--;
            }
            else
            {
                timer1.Enabled = false;
                Security.Program.sec = 60;
                Security.Program.LockWorkStation();
            }
            
        }
    }
}
