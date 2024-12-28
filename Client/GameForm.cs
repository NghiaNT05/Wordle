using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Web;

namespace Client
{
    public partial class GameForm : Form
    {
        private Socket clientSocket = null!;
        private Thread listenerThread = null!;
        private string name = "";
        private string answerCode = null!;
        private int points = 0;
        private int countDown = 150;
        private bool gameProcess = false;
        private int correctLetters = 0;
        private int rowIndex = 0, letterIndex = 0, wordIndex = 0;

        private List<TextBox[]> rows = new List<TextBox[]>();
        private TextBox[] row1 = new TextBox[5];
        private TextBox[] row2 = new TextBox[5];
        private TextBox[] row3 = new TextBox[5];
        private TextBox[] row4 = new TextBox[5];
        private TextBox[] row5 = new TextBox[5];
        private TextBox[] row6 = new TextBox[5];

        public GameForm(string username)
        {
            InitializeComponent();
            name = username;
        }

        private void Connect()
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

        // Handle incoming messages
        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024 * 10];

            while (true)
            {
                try
                {
                    if (!clientSocket.Connected) break;

                    int bytesReceived = clientSocket.Receive(buffer);
                    if (bytesReceived > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                        if (!this.IsDisposed && this.InvokeRequired)
                        {
                            Invoke(new Action(() => ProcessServerMessage(message)));
                        }
                        else
                        {
                            ProcessServerMessage(message);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    MessageBox.Show($"Ban da dang xuat");

                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}");
                    break;
                }
            }

        }

        // Process incoming server messages
        private void ProcessServerMessage(string message)
        {
            string[] token = message.Split(" ");

            if (token[0] == "answer")
            {
                answerCode = token[1];

            }
            else if (token[0] == "niceTry")
            {
                answerCode = token[2];
                MessageBox.Show(token[3]);
            }
            else if (token[0] == "joinsuccess")
            {
                listBox3.Items.Clear();

                string[] player = token[1].Split(",");
                for (int i = 0; i < player.Length; i++)
                {
                    listBox3.Items.Add(player[i]);
                }
                label3.Text = "ROOM : " + comboBox1.SelectedItem.ToString();

            }
            else if (token[0] == "ready")
            {
                string[] player = token[1].Split(",");
                for (int i = 0; i < player.Length; i++)
                {
                    listBox3.Items.Add(player[i] + "đã sẵn sàng");
                }
            }
            else if (token[0] == "leaveSuccess")
            {
                MessageBox.Show("ban da roi phong");
                listBox3.Items.Clear();
                groupBox1.Visible = true;
                button2.Enabled = true;
                label3.Text = "";
            }
            else if (token[0] == "leave")
            {
                listBox3.Items.Add(token[1] + " da roi phong");
                label3.Text = "";
            }
            else if (token[0] == "GameStarting")
            {
                button2.Enabled = false;
                this.KeyPreview = true;
                gameProcess = true;
                timer1.Start();
                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        rows[i][j].Enabled = false;
                    }
                }


            }
            else if (token[0] == "roomplaying")
            {
                MessageBox.Show("phong dang choi vui long chon phong moi ");
                listBox3.Items.Clear();
                groupBox1.Visible = true;
            }
            else if (token[0] == "EndGame")
            {
                button2.Visible = true;
                button2.Enabled = true;
                groupBox1.Visible = true;
                listBox3.Items.Clear();
                label2.Text = "Điểm : ";
                label3.Text = "";
                points = 0;
                ResetAll();
                string[] data = token[1].Split("-");

                string chart = "";

                for (int i = 0; i < data.Length - 1; i++)
                {
                    string[] player = data[i].Split(":");

                    if (player.Length == 3)
                    {
                        chart += player[0] + "  " + player[1] + ": " + player[2] + "\n";
                    }
                    else
                    {
                        // Xử lý trường hợp không có đủ 3 phần tử
                        MessageBox.Show("Dữ liệu không hợp lệ: " + data[i] + "\n");
                    }
                }
                MessageBox.Show(chart);

            }
            else if (token[0] == "EndGamePlayer")
            {

                MessageBox.Show("ban da hoan thanh vui long doi");


            }
            else if (message.StartsWith("Chat"))
            {
                string[] tokenChat = message.Split("/");
                listBox3.Items.Add("[" + tokenChat[2] + "] " + tokenChat[1]);
            }
        }
        private async Task Send(string message)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                await Task.Run(() => clientSocket.Send(data)); // Sử dụng Task để gửi dữ liệu
            }
            else
            {
                MessageBox.Show("Client is not connected to the server.");
            }
        }
        private async void KeyIsUp(object sender, KeyEventArgs e)
        {
            if (letterIndex > 4)
            {
                letterIndex = 4;
            }
            else if (letterIndex < 0)
            {
                letterIndex = 0;
            }
            //Nếu phím gõ vào là ký tự chữ
            if ((e.KeyValue >= 65 && e.KeyValue <= 90))
            {
                if (letterIndex + 1 == 5 && rows[rowIndex][letterIndex].Text != "") ;
                else
                {
                    rows[rowIndex][letterIndex].Text = e.KeyCode.ToString();
                    letterIndex++;

                }

            }
            //Nếu phím gõ vào là phím Enter
            else if (e.KeyCode == Keys.Enter && letterIndex == 4 && rows[rowIndex][4].Text != "")
            {
                if (rowIndex >= 0 && rowIndex <= rows.Count)
                {
                    string answer = "";
                    for (int i = 0; i < 5; i++)
                    {

                        if (i < rows[rowIndex].Length)
                        {
                            char answerC = Convert.ToChar(rows[rowIndex][i].Text[0]);
                            answer += answerC;
                        }
                    }


                    await Send("check" + " " + answer + " " + comboBox1.SelectedItem.ToString() + " " + name + " " + rowIndex.ToString());

                    await WaitForAnswerCode();
                }

                for (int i = 0; i < 5; i++)
                {
                    if (answerCode[i] == '2')
                    {
                        rows[rowIndex][i].BackColor = ColorTranslator.FromHtml("#019A01");
                        rows[rowIndex][i].ForeColor = Color.White;
                        correctLetters++;
                    }
                    else if (answerCode[i] == '1')
                    {
                        rows[rowIndex][i].BackColor = ColorTranslator.FromHtml("#FFC425");
                        rows[rowIndex][i].ForeColor = Color.White;
                    }
                    else if (answerCode[i] == '0')
                    {
                        rows[rowIndex][i].BackColor = ColorTranslator.FromHtml("#444444");
                        rows[rowIndex][i].ForeColor = Color.White;
                    }
                }
                answerCode = null;
                // Kiểm tra kết quả đoán
                if (correctLetters == 5)
                {
                    wordIndex++;
                    if (wordIndex < 5)
                    {
                        countDown += 20;
                    }
                    points++;
                    label2.Text = "Điểm: " + points;
                    ResetAll();
                }
                else if (correctLetters != 5 && rowIndex == 5)
                {
                    wordIndex++;
                    if (wordIndex < 5)
                        countDown -= 20;
                    ResetAll();
                }
                else
                {
                    rowIndex++;
                    letterIndex = 0;
                    correctLetters = 0;
                }
            }
            else if (e.KeyCode == Keys.Back)
            {
                if (letterIndex <= 4 && letterIndex >= 1)
                {
                    if (rows[rowIndex][4].Text != "")
                    {
                        rows[rowIndex][4].Text = "";
                    }
                    else if (letterIndex - 1 < 0) ;
                    else
                    {
                        rows[rowIndex][letterIndex - 1].Text = "";
                        letterIndex--;
                    }
                }
            }
        }

        private async Task WaitForAnswerCode()
        {
            // Đợi cho đến khi nhận được answerCode từ server
            while (answerCode == null)
            {
                await Task.Delay(100); // Dừng tạm thời để không làm treo UI
            }
        }


        // Update colors based on the server response


        // Evaluate and reset game state


        private void ResetAll()
        {
            correctLetters = 0;
            rowIndex = 0;
            letterIndex = 0;
            //Word1
            W1L1.Clear();
            W1L2.Clear();
            W1L3.Clear();
            W1L4.Clear();
            W1L5.Clear();

            W1L1.BackColor = Color.Black;
            W1L2.BackColor = Color.Black;
            W1L3.BackColor = Color.Black;
            W1L4.BackColor = Color.Black;
            W1L5.BackColor = Color.Black;
            //Word2
            W2L1.Clear();
            W2L2.Clear();
            W2L3.Clear();
            W2L4.Clear();
            W2L5.Clear();

            W2L1.BackColor = Color.Black;
            W2L2.BackColor = Color.Black;
            W2L3.BackColor = Color.Black;
            W2L4.BackColor = Color.Black;
            W2L5.BackColor = Color.Black;
            //Word3
            W3L1.Clear();
            W3L2.Clear();
            W3L3.Clear();
            W3L4.Clear();
            W3L5.Clear();

            W3L1.BackColor = Color.Black;
            W3L2.BackColor = Color.Black;
            W3L3.BackColor = Color.Black;
            W3L4.BackColor = Color.Black;
            W3L5.BackColor = Color.Black;

            //Word4
            W4L1.Clear();
            W4L2.Clear();
            W4L3.Clear();
            W4L4.Clear();
            W4L5.Clear();

            W4L1.BackColor = Color.Black;
            W4L2.BackColor = Color.Black;
            W4L3.BackColor = Color.Black;
            W4L4.BackColor = Color.Black;
            W4L5.BackColor = Color.Black;

            //Word5

            W5L1.Clear();
            W5L2.Clear();
            W5L3.Clear();
            W5L4.Clear();
            W5L5.Clear();

            W5L1.BackColor = Color.Black;
            W5L2.BackColor = Color.Black;
            W5L3.BackColor = Color.Black;
            W5L4.BackColor = Color.Black;
            W5L5.BackColor = Color.Black;
            //Word6
            W6L1.Clear();
            W6L2.Clear();
            W6L3.Clear();
            W6L4.Clear();
            W6L5.Clear();

            W6L1.BackColor = Color.Black;
            W6L2.BackColor = Color.Black;
            W6L3.BackColor = Color.Black;
            W6L4.BackColor = Color.Black;
            W6L5.BackColor = Color.Black;

            if (wordIndex == 5)
            {
                wordIndex = 0;
               
                gameProcess = false;

                string room = comboBox1.SelectedItem.ToString();

                Send("EndGame" + " " + name + " " + points.ToString() + " " + room);
               
                points = 0;
            

            }

        }



        private void GameForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
            if (clientSocket != null && clientSocket.Connected)
            {
                clientSocket.Close();
            }
        }

        private void GameForm_Load(object sender, EventArgs e)
        {
            this.KeyPreview = false;


            MessageBox.Show("Welcome to the game!");
            label1.Text = "Tên: " + name;
            label2.Text = "Score: 0";
            Connect();
            Send("hello");
            row1[0] = W1L1;
            row1[1] = W1L2;
            row1[2] = W1L3;
            row1[3] = W1L4;
            row1[4] = W1L5;
            rows.Add(row1);

            row2[0] = W2L1;
            row2[1] = W2L2;
            row2[2] = W2L3;
            row2[3] = W2L4;
            row2[4] = W2L5;
            rows.Add(row2);

            row3[0] = W3L1;
            row3[1] = W3L2;
            row3[2] = W3L3;
            row3[3] = W3L4;
            row3[4] = W3L5;
            rows.Add(row3);

            row4[0] = W4L1;
            row4[1] = W4L2;
            row4[2] = W4L3;
            row4[3] = W4L4;
            row4[4] = W4L5;
            rows.Add(row4);

            row5[0] = W5L1;
            row5[1] = W5L2;
            row5[2] = W5L3;
            row5[3] = W5L4;
            row5[4] = W5L5;
            rows.Add(row5);

            row6[0] = W6L1;
            row6[1] = W6L2;
            row6[2] = W6L3;
            row6[3] = W6L4;
            row6[4] = W6L5;
            rows.Add(row6);

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    rows[i][j].Text = "";
                    rows[i][j].ForeColor = Color.White;
                    rows[i][j].BackColor = Color.Black;
                    rows[i][j].Enabled = false;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            groupBox1.Visible = false;
            string message = "join" + " " + comboBox1.SelectedItem.ToString() + " " + clientSocket + " " + name;
            Send(message);

        }

        private void button2_Click(object sender, EventArgs e)
        {

            string message = "ready" + " " + name + " " + comboBox1.SelectedItem.ToString();
            Send(message);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string message = "leave" + " " + comboBox1.SelectedItem.ToString() + " " + name;
            Send(message);
        }
        private void button4_Click(object sender, EventArgs e)
        {
            string message =  "Send" + "/" + textBox7.Text + "/" + comboBox1.SelectedItem.ToString() + "/" +  name ;
            Send(message);
        }
    }
}
