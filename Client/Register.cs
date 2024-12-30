using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
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

        public Register()
        {
            InitializeComponent();
            InitializeSocket();
        }

        private void InitializeSocket()
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(IPAddress.Parse("127.0.0.1"), 8888);
                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessages));
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối tới server: {ex.Message}");
                clientSocket = null;
            }
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
            // You can add any code here that needs to execute when the form loads
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

        private void ProcessServerMessage(string message)
        {
            if (message.StartsWith("register success"))
            {
                MessageBox.Show("Đăng ký thành công!");
                this.Invoke((MethodInvoker)delegate {
                    this.Close();
                });
            }
            else if (message.StartsWith("register failed"))
            {
                MessageBox.Show("Đăng ký thất bại, vui lòng thử lại.");
            }
            else
            {
                MessageBox.Show("Phản hồi không xác định: " + message);
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024 * 10];
            while (clientSocket.Connected)
            {
                try
                {
                    int bytesReceived = clientSocket.Receive(buffer);
                    if (bytesReceived > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                        this.Invoke(new Action(() => ProcessServerMessage(message)));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối tới server: " + ex.Message);
                    break;
                }
            }
        }

    }
}
