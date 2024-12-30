using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System.Runtime.InteropServices;
namespace Server_Wordles
{
    public partial class Server : Form
    {
        private string answerCode = "";
        private string result = "";
        private int points = 0;
        private int countDown = 150;  // Thời gian đếm ngược
        private int WordIndex = 0;
        private string correctcheck = "";
        Dictionary<int, List<Player>> rooms = new Dictionary<int, List<Player>>();  // Danh sách người chơi trong mỗi phòng
        Dictionary<int, List<KeyValuePair<string, int>>> res = new Dictionary<int, List<KeyValuePair<string, int>>>();  // Kết quả của các người chơi trong phòng
        Dictionary<int, string> Wordstring = new Dictionary<int, string>();  // Các từ đã được chọn trong từng phòng
        Dictionary<int, bool> roomGameStatus = new Dictionary<int, bool>();  // Trạng thái của trò chơi trong các phòng

        // Biến cần thiết khác
        private object roomsLock = new object();
        string[] wordinroom;

        private TcpListener listener = null!;
        private Thread listenerThread = null!;
        private string connectionString = @"Server=localhost;Database=WordleDB;Uid=root;Pwd=12345;";
        List<string> chosenWords = new List<string>();
        //File chứa list từ
        string[] allWords = File.ReadAllText(@"C:\Users\NghiaAo1\Downloads\DA\Wordle\word.txt")
        .Split("\n");
        Random rand = new Random();
        //Random 1 từ
        Dictionary<int, string> roomKeywords = new Dictionary<int, string>();

        public string ChoseWord()
        {
            string currentWord;
            WordIndex = rand.Next(0, allWords.Length);
            currentWord = allWords[WordIndex];
            while (chosenWords.Contains(currentWord))
            {
                WordIndex = rand.Next(0, allWords.Length);
                currentWord = allWords[WordIndex];
            }
            chosenWords.Add(currentWord);
            return currentWord;
        }
        //Random 5 từ
        public string ChoseNWord()
        {
            string words = "";

            for (int i = 0; i < 5; i++)
            {
                words += ChoseWord() + "\n";
            }

            return words;
        }

        public Server()
        {
            InitializeComponent();
            for (int i = 1; i <= 10; i++)
            {
                rooms.Add(i, new List<Player>());
                Wordstring.Add(i, null);
                res.Add(i, new List<KeyValuePair<string, int>>());
                roomGameStatus.Add(i, false);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Kiểm tra nếu máy chủ đã được khởi động
            if (listener != null && listener.Server.IsBound)
            {
                AppendLog("Server is already running.");
                return;
            }

            // Khởi động máy chủ
            listenerThread = new Thread(StartServer);
            listenerThread.IsBackground = true;
            listenerThread.Start();
            AppendLog("Server started. Listening for connections...");
        }


        private async void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();

            while (true)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    AppendLog("Client connected!");
                    HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    AppendLog($"Error: {ex.Message}");
                }
            }
        }

        private async void HandleClientAsync(TcpClient client)
        {
            Player player = null; // Lưu trữ tham chiếu đến người chơi để có thể dễ dàng xóa khi cần thiết
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024 * 10];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AppendLog($"Request received: {request}");
                    string response = ProcessRequest(request, client, ref player); // Chuyển player như một tham chiếu để có thể gán khi người chơi đăng nhập hoặc vào phòng
                    if (response == null)
                    {
                        continue;
                    }
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                AppendLog("Client connection closed.");
                if (player != null)
                {
                    RemovePlayerFromRoom(player);
                }
            }
        }

        private void RemovePlayerFromRoom(Player player)
        {
            foreach (var room in rooms)
            {
                if (room.Value.Contains(player))
                {
                    room.Value.Remove(player);
                    if (room.Value.Count == 0)
                    {
                        rooms.Remove(room.Key);
                        roomGameStatus[room.Key] = false;
                        res.Remove(room.Key);
                        Wordstring.Remove(room.Key);
                        AppendLog($"Room {room.Key} closed due to no players.");
                    }
                    break;
                }
            }
        }


        private string ProcessRequest(string request, TcpClient client, ref Player player)
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
            else if (command == "join")
            {
                string id = parts[1];
                string playerName = parts[3];
                return JoinRoom(id, playerName, client, ref player);
            }
            else if (command == "check")
            {
                AppendLog("Nhận từ đoán ");


                string guessedWord = parts[1];
                string roomid = parts[2];
                string playername = parts[3];
                string attemp = parts[4];
                player = rooms[Int32.Parse(roomid)].FirstOrDefault(p => p.Name == playername);
                string point = player.currentWord.ToString();
                string message = CheckAnswer(guessedWord, roomid, playername, point, attemp);
                if (attemp == "5" || message.Split(" ")[1] == "22222")
                {

                    player.currentWord++;
                }
                if (attemp == "5" && message.Split(" ")[1] != "22222")
                {

                    return "niceTry" + " " + message + " " + correctcheck;
                }

                return message;
            }
            else if (command == "ready")
            {

                string roomid = parts[2];
                string playerName = parts[1];

                ProcessReadyCommand(roomid, playerName);
                return null;

            }
            else if (command == "EndGame")
            {
                string playerName = parts[1];
                string playerPoints = parts[2];
                string roomid = parts[3];
                return HandleEndGame(playerName, playerPoints, roomid);
            }
            else if (command == "leave")
            {
                string roomid = parts[1];
                string playerName = parts[2];
                int roomId = int.Parse(roomid);
                return HandleLeave(roomId, playerName);

            }
            else if (request.StartsWith("Send"))
            {
                string[] token = request.Split("/");
                string message = "Chat" + "/" + token[1] + "/" + token[3];

                int roomid = Int32.Parse(token[2]);

                SendMessageToRoom(roomid, message);
                return null;

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

        private string CheckAnswer(string guessedWord, string roomid, string playername, string point, string attempt)
        {
            int points = int.Parse(point);

            string[] wordinroom = Wordstring[Int32.Parse(roomid)].Split("\n");


            correctcheck = wordinroom[points].ToUpper();
            string answerCode = "";
            bool[] targetUsed = new bool[correctcheck.Length];
            for (int i = 0; i < 5; i++)
            {
                if (guessedWord[i] == correctcheck[i])
                {
                    answerCode += '2';
                    targetUsed[i] = true;
                }
                else
                {
                    answerCode += '-';
                }
            }

            for (int i = 0; i < 5; i++)
            {
                if (answerCode[i] == '-')
                {
                    for (int j = 0; j < 5; j++)
                    {
                        if (!targetUsed[j] && guessedWord[i] == correctcheck[j])
                        {
                            answerCode = answerCode.Substring(0, i) + '1' + answerCode.Substring(i + 1);
                            targetUsed[j] = true;
                            break;
                        }
                    }
                }
            }

            // Finally, assign '0' for the incorrect letters (gray)
            for (int i = 0; i < 5; i++)
            {
                if (answerCode[i] == '-')  // If the letter is still a placeholder (not green or yellow)
                {
                    answerCode = answerCode.Substring(0, i) + '0' + answerCode.Substring(i + 1);  // Incorrect letter (gray)
                }
            }

            return "answer " + answerCode;  // Return the final answer code
        }

        private int checkreword(string word, char gess)
        {
            int tmp = 0;
            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] == gess)
                {
                    tmp++;
                }
            }
            return tmp;
        }
        private string JoinRoom(string id, string playerName, TcpClient client, ref Player player)
        {
            int roomId = int.Parse(id);
            lock (roomsLock)
            {

                if (!rooms.ContainsKey(roomId) || !roomGameStatus[roomId])
                {
                    // Tạo phòng mới nếu phòng cũ đã đóng hoặc không tồn tại
                    rooms[roomId] = new List<Player>();
                    roomGameStatus[roomId] = true;
                    Wordstring[roomId] = ChoseNWord();
                }
                if (rooms[roomId].Any(p => p.Name == playerName))
                {
                    return "Player with the same name already exists in the room.";
                }
                if (rooms[roomId].Count < 4)
                {
                    player = new Player(playerName, client);
                    rooms[roomId].Add(player);
                    string playersInRoom = string.Join(",", rooms[roomId].Select(p => p.Name));
                    SendMessageToRoom(roomId, $"joinsuccess {playersInRoom} {roomId}");
                    return null;
                }
                else
                {
                    return "Room is full";
                }
            }
        }

        private void ProcessReadyCommand(string RoomId, string playerName)
        {
            int roomId = int.Parse(RoomId);
            if (rooms.ContainsKey(roomId))
            {
                Player player = rooms[roomId].FirstOrDefault(p => p.Name == playerName);
                if (player != null)
                {
                    // Đánh dấu người chơi là đã sẵn sàng
                    player.IsReady = true;

                    // Kiểm tra nếu tất cả người chơi đã sẵn sàng
                    bool allReady = rooms[roomId].All(p => p.IsReady);

                    if (allReady)
                    {
                        string tmp = ChoseNWord();
                        AppendLog(tmp);
                        Wordstring[roomId] = tmp;
                        roomKeywords[roomId] = tmp;  // Lưu từ khóa cho phòng
                        roomGameStatus[roomId] = true;
                        SendMessageToRoom(roomId, "GameStarting");

                        UpdateKeywordDisplay();  // Cập nhật hiển thị từ khóa trên giao diện server
                    }
                    else
                    {
                        // Nếu còn người chưa sẵn sàng, gửi tin nhắn cho phòng
                        SendMessageToRoom(roomId, $"ready {playerName}");
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }


        private void UpdateKeywordDisplay()
        {
            var keywordDisplay = new StringBuilder();
            foreach (var room in roomKeywords)
            {
                keywordDisplay.AppendLine($"Room {room.Key}:");
                var words = room.Value.Split('\n');
                foreach (var word in words)
                {
                    keywordDisplay.AppendLine($"\t{word.Trim()}");
                }
            }
            AppendLog("Current keywords for each room:");
            AppendLog(keywordDisplay.ToString());
        }


        private string HandleLeave(int roomid, string name)
        {
            if (rooms.ContainsKey(roomid))
            {
                Player player = rooms[roomid].FirstOrDefault(p => p.Name == name);
                if (player != null)
                {
                    rooms[roomid].Remove(player);
                    SendMessageToRoom(roomid, "leave" + " " + name);
                    if (rooms[roomid].Count == 0)
                    {
                        rooms.Remove(roomid);
                        roomGameStatus.Remove(roomid);
                        res.Remove(roomid);
                        Wordstring.Remove(roomid);
                        AppendLog($"Room {roomid} has been closed due to no remaining players.");
                    }
                    return "leaveSuccess";
                }
                else
                {
                    return "leaveFailed";
                }
            }
            else
            {
                return "leaveFailed";
            }
        }


        private string HandleEndGame(string playerName, string playerPoints, string roomid)
        {
            if (Int32.TryParse(roomid, out int roomId))
            {
                lock (roomsLock)
                {
                    if (rooms.ContainsKey(roomId))
                    {
                        Player player = rooms[roomId].FirstOrDefault(p => p.Name == playerName);

                        if (player != null)
                        {
                            res[roomId].Add(new KeyValuePair<string, int>(playerName, Int32.Parse(playerPoints)));
                            if (res[roomId].Count == rooms[roomId].Count)
                            {
                                string result = "";

                                foreach (KeyValuePair<string, int> item in res[roomId])
                                {
                                    result += "Player:" + item.Key + "Point:" + item.Value + "-";

                                }
                                SendMessageToRoom(roomId, "EndGame" + " " + result);
                                rooms[roomId].Clear();
                                res[roomId].Clear();

                                roomGameStatus[roomId] = false;
                                player.Name = null;
                                player.Client = null;
                                player.IsReady = false;
                                return null;


                            }
                            else
                            {

                                return "EndGamePlayer" + " " + playerName + " " + playerPoints + " " + roomid;
                            }

                        }
                        else
                        {
                            // Nếu không tìm thấy người chơi trong phòng
                            AppendLog($"Player {playerName} not found in room {roomId}");
                            return null;
                        }
                    }
                    else
                    {
                        // Nếu roomId không tồn tại trong rooms
                        AppendLog($"Room ID {roomId} not found");
                        return null;
                    }
                }
            }
            else
            {
                // Nếu roomid không hợp lệ
                AppendLog($"Invalid Room ID: {roomid}");
                return null;
            }
        }
        private async void SendMessageToRoom(int roomId, string message)
        {
            if (rooms.ContainsKey(roomId))
            {
                foreach (var player in rooms[roomId])
                {
                    try
                    {
                        NetworkStream stream = player.Client.GetStream();
                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error sending message to {player.Name}: {ex.Message}");
                    }
                }
            }
            else
            {
                AppendLog($"Room {roomId} not found.");
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
            for (int i = 1; i <= 10; i++)
            {
                rooms.Remove(i);
                roomGameStatus.Remove(i);

            }

        }
    }
    public class Player
    {
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public bool IsReady { get; set; }
        public int currentWord { get; set; }

        public Player(string name, TcpClient client)
        {
            Name = name;
            Client = client;
            IsReady = false;
            currentWord = 0;

        }
    }
}
