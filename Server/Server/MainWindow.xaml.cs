using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    public partial class MainWindow : Window
    {
        private bool buttonIsStart;
        private TcpListener serverSocket;
        private Thread serverThread;
        private ManualResetEvent eventStop;
        private List<ClientInfo> listClients;

        private class ClientInfo
        {
            private TcpClient Client { get; set; }
            public string Name { get; set; }
            public ClientInfo()
            {
                Client = new TcpClient();
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            ipAddress.Items.Add("0.0.0.0");
            ipAddress.Items.Add("127.0.0.1");
            var entryServer = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in entryServer.AddressList)
            {
                ipAddress.Items.Add(ip.ToString());
            }

            buttonIsStart = false;
            serverSocket = null;
            serverThread = null;
            listClients = new List<ClientInfo>();
            eventStop = new ManualResetEvent(false);
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (!buttonIsStart)
            {
                try
                {
                    serverSocket = new TcpListener(IPAddress.Parse(ipAddress.Text), int.Parse(port.Text));
                    serverSocket.Start(100);
                    serverThread = new Thread(ServerThreadProc);
                    serverThread.Start(serverSocket);
                    buttonIsStart = true;
                    buttonStart.Content = "Stop";
                }
                catch (Exception exctption)
                {
                    MessageBox.Show(exctption.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            }
        }

        private void CloseButonClick(object sender, RoutedEventArgs e)
        {

        }

        private void ServerThreadProc(object obj)
        {
            TcpListener serverSoocketSecond = (TcpListener)obj;
            while (true)
            {
                // 1
                //TcpClient client = serverSoocketSecond.AcceptTcpClient();
                //ThreadPool.QueueUserWorkItem(ClientThreadProc, client);

                // 2
                IAsyncResult asyncResult = serverSoocketSecond.BeginAcceptSocket(AsyncServerProc, serverSocket);
                while(asyncResult.AsyncWaitHandle.WaitOne(200) == false)
                {
                    if (eventStop.WaitOne(0) == true)
                        return;
                }

            }
        }
        private void WriteToLog(string str)
        {
            Dispatcher.Invoke(() => txtLog.AppendText(str));
        }

        private void AsyncServerProc(IAsyncResult iAsync)
        {
            TcpListener serverSocket = (TcpListener)iAsync.AsyncState;
            TcpClient client =  serverSocket.EndAcceptTcpClient(iAsync);
            WriteToLog("Подключился клиент");
            WriteToLog("IP адрес клиента" + client.Client.RemoteEndPoint.ToString() + "\n");
            ThreadPool.QueueUserWorkItem(ClientThreadProc, client);
            
        }

        private void ClientThreadProc(object obj)
        {
            TcpClient client = (TcpClient)obj;
            WriteToLog("Рабочий поток клиента запущен");
            var buffer = new byte[1024 * 4];
            string clientName;
            int reciveSize = client.Client.Receive(buffer);
            clientName = Encoding.UTF8.GetString(buffer);
            WriteToLog($"Клиент: {clientName} \r\n");
            client.Client.Send(Encoding.ASCII.GetBytes($"Hello {clientName}"));
            while (true)
            {
                reciveSize = client.Client.Receive(buffer);
                string message = Encoding.UTF8.GetString(buffer);
                client.Client.Send(Encoding.ASCII.GetBytes(message));
            }
        }
    }
}
