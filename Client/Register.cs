using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;


namespace Client
{
    public partial class Register : Form
    {
        private Socket clientSocket;
        public Register(Socket Socket)
        {
            InitializeComponent();
            clientSocket = Socket;
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                StringBuilder hexString = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hexString.Append(b.ToString("x2"));
                }
                return hexString.ToString();
            }
        }

        private void Register_Load(object sender, EventArgs e)
        {

        }

        private void Send(string message)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                clientSocket.Send(data);
            }
            else
            {
                MessageBox.Show("Client is not connected to the server.");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (clientSocket == null || !clientSocket.Connected)
            {
                MessageBox.Show("Lỗi khi kết nối tới server. Vui lòng khởi động lại.");
                return;
            }

            if (string.IsNullOrEmpty(textBox1.Text) || string.IsNullOrEmpty(textBox2.Text) ||
                string.IsNullOrEmpty(textBox3.Text) || string.IsNullOrEmpty(textBox4.Text) ||
                string.IsNullOrEmpty(comboBox1.Text))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin.");
                return;
            }

            string username = textBox1.Text;
            string password = HashPassword(textBox2.Text);
            string email = textBox3.Text;
            string phone = textBox4.Text;
            string gender = comboBox1.Text;

            // Gửi yêu cầu đăng ký tới server
            string message = $"register {username} {password} {email} {phone} {gender}";
            Send(message);
        }
    }
}
