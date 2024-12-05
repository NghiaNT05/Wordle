using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading;


namespace Client

{
    public partial class Form1 : Form
    {
        private Socket clientSocket = null!;
        private Thread listenerThread = null!;
        private Register registerForm = null!;

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
            if (message.StartsWith("login success"))
            {
                
                GameForm gameForm = new GameForm(textBox1.Text);
                gameForm.Show();
                this.Hide();
            }
            else if (message.StartsWith("register success"))
            {
                MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.");
                registerForm?.Close();
            }
            else if (message.StartsWith("login failed"))
            {
                MessageBox.Show("Đăng nhập thất bại. Vui lòng kiểm tra lại.");
            }
            else if (message.StartsWith("register failed"))
            {
                MessageBox.Show("Đăng ký thất bại. Vui lòng thử lại.");
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
            if (clientSocket == null || !clientSocket.Connected)
            {
                MessageBox.Show("Lỗi khi kết nối tới server. Vui lòng khởi động lại.");
                return;
            }

            if (string.IsNullOrEmpty(textBox1.Text) || string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin đăng nhập.");
                return;
            }

            string username = textBox1.Text;
            string password = HashPassword(textBox2.Text);

            // Gửi yêu cầu đăng nhập tới server
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
            try
            {
                conection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối đến server: {ex.Message}");
            }

        }
    }
}
