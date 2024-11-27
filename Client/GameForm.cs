using System;
using System.Windows.Forms;

namespace Client
{
    public partial class GameForm : Form
    {
        public GameForm()
        {
            InitializeComponent();
        }

        private void GameForm_Load(object sender, EventArgs e)
        {
            // Logic khi trò chơi bắt đầu
            MessageBox.Show("Welcome to the game!");
        }
    }
}
