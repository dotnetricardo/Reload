namespace ShapeFX.Reload.Forms
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;


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
