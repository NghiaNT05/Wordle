using System;
using System.Windows.Forms;

namespace Client
{
    public partial class Menu : Form
    {
        public Menu()
        {
            InitializeComponent();
        }

        private void btnOpenServer_Click_1(object sender, EventArgs e)
        {
            Server.Form1 serverForm = new Server.Form1();
            serverForm.Show();
        }

        private void btnOpenClient_Click_1(object sender, EventArgs e)
        {
            Client.Form1 clientForm = new Client.Form1();
            clientForm.Show();
        }
    }
}
