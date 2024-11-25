using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading;


namespace Client

{
    public partial class Form1 : Form
    {
        private Socket clientSocket;
        private Thread listenerThread;
        private Register registerForm;

        public Form1()
        {
            InitializeComponent();

        }
        private void conection()
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(IPAddress.Parse("127.0.0.1"), 8888);
                listenerThread = new Thread(ReceiveMessages);
                listenerThread.IsBackground = true;
                listenerThread.Start();
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}");
            }
        }
        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (clientSocket.Connected)
            {
                try
                {
                    int bytesReceived = clientSocket.Receive(buffer);
                    if (bytesReceived > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                        Invoke(new Action(() => ProcessServerMessage(message)));
                    }
                }
                catch (SocketException ex)
                {
                    MessageBox.Show($"Socket error: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}");
                    break;
                }
            }
        }

        private void ProcessServerMessage(string message)
        {
            if (message == "Login Successfull")
            {
                Menu Room = new Menu();
                Room.Show();
            }
            else if (message == "register successfull")
            {
                registerForm.Close();
            }
            else
            {
                MessageBox.Show(message);
            }
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
            if (clientSocket == null || !clientSocket.Connected || string.IsNullOrEmpty(textBox1.Text) || string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Lỗi khi kết nối tới server vui lòng khởi động lại");
            }
            string username = textBox1.Text;
            string password = HashPassword(textBox2.Text);
            string message = $"login {username} {password}";
            Send(message);

        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            registerForm = new Register(clientSocket);
            registerForm.Show();

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

        private void Form1_Load(object sender, EventArgs e)
        {
            conection();

        }
    }
}
