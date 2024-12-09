using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
namespace Server
{
    public partial class Form1 : Form
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
        private string connectionString = @"Server=localhost;Database=WordleDB;Uid=root;Pwd=1234;";
        List<string> chosenWords = new List<string>();
        //File chứa list từ
        string[] allWords = File.ReadAllText(@"D:\DA\Wordle\Server\word.txt")
      .Split("\n");
        Random rand = new Random();
        //Random 1 từ
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
            MessageBox.Show(words);

            return words;
        }
        

        public Form1()
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
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024 * 10];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AppendLog($"Request received: {request}");
                    string response = ProcessRequest(request, client);
                    if(response == null)
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
            }
        }


        private string ProcessRequest(string request, TcpClient client)
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
                return JoinRoom(id, playerName,client);
            }
            else if (command == "check")
            {
                AppendLog("Nhận từ đoán ");
                

                string guessedWord = parts[1];
                string roomid = parts[2];
                string playername = parts[3];
                string attemp = parts[4];
                Player player = rooms[Int32.Parse( roomid)].FirstOrDefault(p => p.Name == playername);
                string point = player.currentWord.ToString();
                string message = CheckAnswer(guessedWord, roomid, playername, point, attemp);
                if (attemp == "5" || message.Split(" ")[1] == "22222" )
                {

                    player.currentWord++;
                }
                if (attemp == "5" && message.Split(" ")[1] != "22222")
                {
                    
                    return "niceTry" + " " + message + " " + correctcheck;
                }

                    return message;
            }
            else if(command == "ready")
            {
                
                string roomid =parts[2];
                string playerName = parts[1];

                ProcessReadyCommand(roomid, playerName);
                return null;

            }
            else if (command == "EndGame")
            {
                string playerName = parts[1];
                string playerPoints = parts[2];
                string roomid = parts[3];
              return  HandleEndGame(playerName, playerPoints, roomid);
            }
            else if (command == "leave")
            {
                string roomid =parts[1];
                string playerName = parts[2];
                int roomId = int.Parse(roomid);
                return HandleLeave(roomId, playerName);

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
      
        private string CheckAnswer(string guessedWord, string roomid, string playername, string point, string attemp)
        {
            int points = int.Parse(point);
             wordinroom = Wordstring[Int32.Parse(roomid)].Split("\n");
            
            correctcheck = wordinroom[points].ToUpper();
            answerCode = "";

            for (int i = 0; i < 5; i++)
            {
                if (guessedWord[i] == correctcheck[i])
                {
                    answerCode += '2';  // Correct letter in correct position
                }
                else if (correctcheck.Contains(guessedWord[i]))
                {
                    if (checkreword(correctcheck, guessedWord[i]) >= checkreword(guessedWord, guessedWord[i])){
                        answerCode += '1';
                    }
                    else
                    {
                        answerCode += '0';
                    }

                }
                else
                {
                    answerCode += '0';  // Incorrect letter
                }
            }

            
            return  "answer" + " " +answerCode;
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
        private string JoinRoom(string id, string playerName, TcpClient client)
        {
            int roomId = int.Parse(id);

           
            lock (roomsLock)
            {
                if (roomGameStatus.ContainsKey(roomId) && roomGameStatus[roomId] == true)
                {
                    string mes ="";
                    foreach (var i in rooms[roomId])
                    {
                        mes += i.Name + " ";

                    }
                    return "roomplaying" + " " + mes;

                }
                // Kiểm tra nếu playerName đã tồn tại trong phòng
                if (rooms.ContainsKey(roomId))
                {
                    if (rooms[roomId].Any(p => p.Name == playerName))
                    {
                        return "Player with the same name already exists in the room.";
                    }
                }

                // Nếu phòng chưa có thì tạo mới
                if (!rooms.ContainsKey(roomId))
                {
                    rooms[roomId] = new List<Player>();
                }

                // Nếu phòng chưa đầy thì thêm player vào phòng
                if (rooms[roomId].Count < 4)  
                {
                    rooms[roomId].Add(new Player(playerName, client));  // Thêm player vào phòng

                    // Trả về danh sách player trong phòng
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
                        Wordstring[roomId] = tmp ;
                        roomGameStatus[roomId] = true;
                        SendMessageToRoom(roomId, "GameStarting");
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
        private string HandleLeave(int roomid, string name)
        {
            if (rooms.ContainsKey(roomid))
            {
                Player player = rooms[roomid].FirstOrDefault(p => p.Name == name);
                if (player != null)
                {
                    rooms[roomid].Remove(player);
                    
                    if (rooms[roomid].Count == 0)
                    {
                        rooms.Remove(roomid);
                        roomGameStatus.Remove(roomid);
                        res.Remove(roomid);
                        Wordstring.Remove(roomid);
                    }
                    SendMessageToRoom(roomid, "leave" + " " + name);
                    return "leaveSuccess" ;
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
        private string HandleEndGame ( string playerName, string playerPoints, string roomid)
        {
            if (Int32.TryParse(roomid, out int roomId))
            {
                // Đảm bảo rằng thao tác với các tài nguyên dùng chung được đồng bộ
                lock (roomsLock)
                {
                    // Kiểm tra nếu roomId tồn tại trong rooms
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
                // Lặp qua tất cả người chơi trong phòng và gửi tin nhắn
                foreach (var player in rooms[roomId])
                {
                    try
                    {
                        NetworkStream stream = player.Client.GetStream();
                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                        // Gửi tin nhắn tới client
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        // Nếu có lỗi khi gửi tin nhắn, log lỗi
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
            for(int i  = 1; i <= 10; i++)
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
