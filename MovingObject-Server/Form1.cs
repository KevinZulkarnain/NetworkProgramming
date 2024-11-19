using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MovingObject
{
    public partial class Form1 : Form
    {
        Pen red = new Pen(Color.Red);
        Rectangle rect = new Rectangle(20, 20, 30, 30);
        SolidBrush fillBlue = new SolidBrush(Color.Blue);
        int slide = 10;
        TcpListener server;
        Thread serverThread;
        ConcurrentDictionary<TcpClient, NetworkStream> clients = new ConcurrentDictionary<TcpClient, NetworkStream>();

        public Form1()
        {
            InitializeComponent();
            timer1.Interval = 50;
            timer1.Enabled = true;

            // Start the server on a new thread
            serverThread = new Thread(StartServer);
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        private void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 12345);
            server.Start();

            while (true)
            {
                try
                {
                    // Accept client connection
                    TcpClient client = server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    // Add the client to the dictionary
                    clients.TryAdd(client, stream);

                    // Start a new thread to handle the client
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Server Error: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = clients[client];

            try
            {
                while (client.Connected)
                {
                    // Send the position of the rectangle to this client
                    string message = rect.X.ToString();
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                    Thread.Sleep(50); // Delay to control update frequency
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Client Handling Error: {ex.Message}");
            }
            finally
            {
                // Remove the client from the dictionary and close the connection
                clients.TryRemove(client, out _);
                client.Close();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            back();
            rect.X += slide;
            Invalidate();
        }

        private void back()
        {
            if (rect.X >= this.Width - rect.Width * 2)
                slide = -10;
            else if (rect.X <= rect.Width / 2)
                slide = 10;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawRectangle(red, rect);
            g.FillRectangle(fillBlue, rect);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the server and all client threads
            server.Stop();
            foreach (var client in clients.Keys)
            {
                client.Close();
            }
            serverThread.Abort();
        }
    }
}
