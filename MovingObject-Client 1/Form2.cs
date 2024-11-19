using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MovingObject
{
    public partial class Form2 : Form
    {
        Pen red = new Pen(Color.Red);
        Rectangle rect = new Rectangle(20, 20, 30, 30);
        SolidBrush fillBlue = new SolidBrush(Color.Blue);
        TcpClient client;
        Thread clientThread;
        private bool isRunning = true;  // Flag to safely stop the client thread

        public Form2()
        {
            InitializeComponent();
            timer1.Interval = 50;
            timer1.Enabled = true;

            // Start client connection to server
            clientThread = new Thread(StartClient);
            clientThread.IsBackground = true;
            clientThread.Start();
        }

        private void StartClient()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 12345);
                NetworkStream stream = client.GetStream();

                while (isRunning)
                {
                    byte[] data = new byte[256];
                    int bytesRead = stream.Read(data, 0, data.Length);
                    string message = Encoding.ASCII.GetString(data, 0, bytesRead);

                    // Parse the received rectangle position
                    if (int.TryParse(message, out int rectX))
                    {
                        // Safely update the UI from the background thread
                        this.Invoke((MethodInvoker)delegate
                        {
                            rect.X = rectX;
                            Invalidate(); // Trigger the repaint of the form
                        });
                    }

                    Thread.Sleep(50); // Delay to control update frequency
                }

                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void Form2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draw rectangle
            g.DrawRectangle(red, rect);
            g.FillRectangle(fillBlue, rect);

            // Draw text at the center
            Font font = new Font("Arial", 12);
            SolidBrush textBrush = new SolidBrush(Color.Black);
            string text = "client 1";
            SizeF textSize = g.MeasureString(text, font);
            float textX = (this.ClientSize.Width - textSize.Width) / 2;
            float textY = (this.ClientSize.Height - textSize.Height) / 2;
            g.DrawString(text, font, textBrush, new PointF(textX, textY));
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the client thread safely
            isRunning = false;
            clientThread.Join();  // Wait for the thread to finish
        }
    }
}
