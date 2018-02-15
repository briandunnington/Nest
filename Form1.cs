using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xipton.Razor;
using Xipton.Razor.Core;
using Xipton.Razor.Config;
using Xipton.Razor.Extension;
using MarkdownSharp;
using System.IO;

namespace Nest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label4.Visible = false;

            string inputPath = this.textBox1.Text;

            var generator = new Generator(inputPath);
            generator.RegisterPropertyConverter("Date", BasicConverters.DateConverter);
            generator.Generate();

            label4.Visible = true;
        }
    }
}
