using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Server
{
    public partial class Form1 : Form
    {
        private TcpListener listener = null!;
        private Thread listenerThread = null!;

        private string connectionString = "Server=localhost;Database=WordleDB;Uid=root;Pwd=Sql1124@;";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            listenerThread = new Thread(StartServer);
            listenerThread.IsBackground = true;
            listenerThread.Start();
            AppendLog("Server started. Listening for connections...");
        }

        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();

            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    AppendLog("Client connected!");

                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        AppendLog($"Request received: {request}");
                        string response = ProcessRequest(request);
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                    }

                    client.Close();
                }
                catch (Exception ex)
                {
                    AppendLog($"Error: {ex.Message}");
                }
            }
        }

        private string ProcessRequest(string request)
        {
            string[] parts = request.Split(' ');
            string command = parts[0];

            if (command == "register")
            {
                string username = parts[1];
                string password = parts[2];
                string email = parts[3];
                string phone = parts[4];
                string gender = parts[5];

                return RegisterUser(username, password, email, phone, gender)
                    ? "register success"
                    : "register failed";
            }
            else if (command == "login")
            {
                string username = parts[1];
                string password = parts[2];

                return LoginUser(username, password) ? "login success" : "login failed";
            }

            return "invalid command";
        }

        private bool RegisterUser(string username, string password, string email, string phone, string gender)
        {
            string query = "INSERT INTO Users (Username, PasswordHash, Email, Phone, Gender) VALUES (@Username, @PasswordHash, @Email, @Phone, @Gender)";

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@PasswordHash", password);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Phone", phone);
                        command.Parameters.AddWithValue("@Gender", gender);

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Database error: {ex.Message}");
                    return false;
                }
            }
        }

        private bool LoginUser(string username, string password)
        {
                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND PasswordHash = @PasswordHash";

                using (var connection = new MySqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Username", username);
                            command.Parameters.AddWithValue("@PasswordHash", password);

                            int count = Convert.ToInt32(command.ExecuteScalar());
                            return count > 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Database error: {ex.Message}");
                        return false;
                    }
                }
            
        }

        private void AppendLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(message)));
                return;
            }

            txtLog.AppendText($"{DateTime.Now}: {message}\r\n");
        }

        private void TestDatabaseConnection()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    AppendLog("Database connected successfully!");
                }
                catch (Exception ex)
                {
                    AppendLog($"Database connection error: {ex.Message}");
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TestDatabaseConnection();
        }
    }
}
