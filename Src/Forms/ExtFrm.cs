using System;
using System.Windows.Forms;

namespace Reload.Forms
{
    public partial class ExtFrm : Form
    {
        public ExtFrm()
        {
            this.InitializeComponent();
            this.extensionsBox.Text = Statics.Extensions;
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            Statics.Extensions = this.extensionsBox.Text;
        }

    }
}
