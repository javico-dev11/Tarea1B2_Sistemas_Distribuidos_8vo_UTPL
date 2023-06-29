
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server_GC_SD
{
    public partial class Form1 : Form
    {
        Server server;
        LogicalVectorClock globalClock = new LogicalVectorClock(4, null);

        public Form1()
        {
            InitializeComponent();
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            int serverPort = int.Parse(txtPuerto.Text);

            server = new Server();
            server.MessageReceived += Server_MessageReceived;
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;

            // Crea un hilo separado para iniciar el servidor
            Thread serverThread = new Thread(() => server.Start(serverPort));
            serverThread.Start();


            // Actualizar la interfaz de usuario para indicar que el servidor está iniciado
            btnStartServer.Enabled = false;
            lblServerStatus.Text = "Server Iniciado";
        }

        private void Server_ClientConnected(object sender, string clientInfo)
        {
            // Actualizar el ListBox con el cliente conectado
            lstClientes.Invoke((Action)(() => lstClientes.Items.Add(clientInfo)));
        }

        private void Server_ClientDisconnected(object sender, string clientInfo)
        {
            // Actualizar el ListBox con el cliente desconectado
            lstClientes.Invoke((Action)(() => lstClientes.Items.Remove(clientInfo)));
        }

        private void Server_MessageReceived(object sender, MessageEventArgs e)
        {
            var auxMsg = e.Message.Remove(e.Message.Length -7);

            #region modificar el vector global
            if (e.Clock.vector[0] > 0) globalClock.vector[0] = e.Clock.vector[0];
            if (e.Clock.vector[1] > 0) globalClock.vector[1] = e.Clock.vector[1];
            if (e.Clock.vector[2] > 0) globalClock.vector[2] = e.Clock.vector[2];
            if (e.Clock.vector[3] > 0) globalClock.vector[3] = e.Clock.vector[3];
            #endregion

            string newMsg = $"{auxMsg} {globalClock.ToString()}";

            // Actualiza la interfaz de usuario para mostrar el mensaje recibido
            Invoke((MethodInvoker)(() => lstMessages.Items.Add(newMsg)));

            // Enviar el mensaje a todos los clientes conectados
            server.BroadcastMessage(newMsg, globalClock);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblFechaHora.Text = DateTime.Now.ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }

    #region clase server
    public class Server
    {
        private TcpListener listener;
        private Thread acceptThread;
        private Thread receiveThread;
        private List<TcpClient> connectedClients;
        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<string> ClientConnected;
        public event EventHandler<string> ClientDisconnected;

        public Server()
        {
            connectedClients = new List<TcpClient>();
        }

        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            AcceptClients();
        }

        public void BroadcastMessage(string message, LogicalVectorClock clock)
        {
            // Crea el mensaje con el contenido y el reloj vectorial lógico
            Message msg = new Message(message, clock);

            // Convierte el mensaje a una cadena de bytes para enviarlo a través de la red
            byte[] data = Encoding.ASCII.GetBytes(msg.ToString());

            // Envía el mensaje a todos los clientes conectados
            lock (connectedClients)
            {
                foreach (var client in connectedClients)
                {
                    client.GetStream().Write(data, 0, data.Length);
                }
            }
        }

        private void AcceptClients()
        {
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                // Agregar el cliente a la lista de clientes conectados
                lock (connectedClients)
                {
                    connectedClients.Add(client);

                    // Disparar el evento de cliente conectado
                    ClientConnected?.Invoke(this, client.Client.RemoteEndPoint.ToString());
                }

                // Crea un nuevo hilo para manejar la comunicación con el cliente
                Thread receiveThread = new Thread(() => ReceiveMessages(client));
                receiveThread.Start();
            }
        }

        private void ReceiveMessages(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);

                    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    var aux = receivedData.Split();
                    var auxClockString = aux[aux.Length-1];
                    var auxClock = Array.ConvertAll(auxClockString.Split(','), s => int.Parse(s));

                    LogicalVectorClock clock = new LogicalVectorClock(auxClock.Length, auxClock);

                    // Parsea los datos recibidos y extrae el mensaje y el reloj vectorial lógico
                    Message msg = Message.Parse(receivedData);

                    // Dispara el evento MessageReceived para notificar al formulario del servidor
                    OnMessageReceived(msg.Content, clock);
                }
                catch (Exception ex)
                {
                    // Ocurrió un error durante la recepción de mensajes
                    Console.WriteLine("Error al recibir mensajes: " + ex.Message);
                    break;
                }
            }
        }

        private void OnMessageReceived(string message, LogicalVectorClock clock)
        {
            // Verifica que el evento no sea nulo antes de invocarlo
            MessageReceived?.Invoke(this, new MessageEventArgs(message, clock));
        }
    }
    #endregion

    #region clase control mensajes
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }
        public LogicalVectorClock Clock { get; }

        public MessageEventArgs(string message, LogicalVectorClock clock)
        {
            Message = message;
            Clock = clock;
        }
    }
    #endregion

    #region clase reloj vectorial
    public class LogicalVectorClock
    {
        public int[] vector { get; set; }

        public LogicalVectorClock(int size, int[] arraClock)
        {
            vector = new int[size];
            if (arraClock != null)
            {
                vector = arraClock;
            }
        }

        public void Tick(int index)
        {
            vector[index]++;
        }

        public void UpdateClock(LogicalVectorClock otherClock)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] = Math.Max(vector[i], otherClock.vector[i]);
            }
        }

        public static LogicalVectorClock Parse(string clockStr)
        {
            // Verificar si la cadena es nula o vacía
            if (string.IsNullOrEmpty(clockStr))
                throw new ArgumentException("Cadena de reloj inválido!.");

            // Separar la cadena en partes
            string[] parts = clockStr.Split(',');

            // Crear un arreglo de enteros para el reloj vectorial lógico
            int[] clock = new int[parts.Length];

            // Parsear y asignar los valores del reloj vectorial lógico
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int value))
                    throw new ArgumentException("Cadena de reloj inválido!.");

                clock[i] = value;
            }

            // Crear una instancia de LogicalVectorClock con el reloj parseado
            return new LogicalVectorClock(clock.Length, clock);
        }

        public override string ToString()
        {
            // Convierte el reloj vectorial lógico a una cadena
            return string.Join(",", vector);
        }
    }
    #endregion

    #region clase modelo mensaje clock
    public class Message
    {
        public string Content { get; }
        public LogicalVectorClock Clock { get; }

        public Message(string content, LogicalVectorClock clock)
        {
            Content = content;
            Clock = clock;
        }

        public override string ToString()
        {
            // Convierte el mensaje y el reloj vectorial lógico a una cadena
            return $"{Content} {Clock}";
        }

        public static Message Parse(string data)
        {
            // Parsea una cadena de datos y devuelve un objeto Message con el contenido y el reloj vectorial lógico
            return new Message(data, null);
        }
    }
    #endregion
}
