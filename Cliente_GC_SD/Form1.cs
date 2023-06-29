
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cliente_GC_SD
{
    public partial class Form1 : Form
    {
        private Client client;
        private LogicalVectorClock clock;
        private int countEvent = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            string serverIP = txtServerIP.Text;
            int serverPort = int.Parse(txtServerPort.Text);

            client = new Client();
            client.MessageReceived += Client_MessageReceived;
            client.Connect(serverIP, serverPort);

            // Actualizar la interfaz de usuario para indicar que el cliente está conectado
            btnConnect.Enabled = false;
            lblStatus.Text = "Conectado";

            // Obtener el número de procesos conocidos (por ejemplo, la cantidad de clientes conectados)
            int numProcesosConocidos = ObtenerNumeroProcesosConocidos();

            // Inicializar el reloj vectorial lógico con el tamaño adecuado
            clock = new LogicalVectorClock(numProcesosConocidos, null);
        }

        private int ObtenerNumeroProcesosConocidos()
        {
            //por ahora sólo 4 procesos
            return 4;
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text;
            int process = Convert.ToInt32(txtProceso.Text);
            countEvent++;

            string strCountEvent;

            if (countEvent < 10)
            {
                strCountEvent = $"0{countEvent}";
            }
            else
            {
                strCountEvent = $"{countEvent}";
            }

            string nameProcess = $"PROCESO:{process}";
            string fullMessage = $"{nameProcess} - EVENTO:{strCountEvent} - MSG:<<{message}>> => VECTOR:";
            // Enviar el mensaje al servidor y actualizar el reloj vectorial lógico
            clock.Tick(process-1); // El índice 0 representa el proceso actual
            client.SendMessage(fullMessage, clock);

            // Limpiar el TextBox del mensaje
            txtMessage.Text = "";
        }

        private void Client_MessageReceived(object sender, MessageEventArgs e)
        {

            var aux = e.Message.Split();
            var auxClockString = aux[aux.Length -1];

            //auxClockString = auxClockString.Remove(auxClockString.Length - 7);

            var auxClock = Array.ConvertAll(auxClockString.Split(','), s => int.Parse(s));

            LogicalVectorClock clock = new LogicalVectorClock(auxClock.Length, auxClock);
            // Actualizar el reloj vectorial lógico con el reloj recibido del servidor
            clock.UpdateClock(clock);

            // Mostrar el mensaje recibido en el ListBox
            //lstMessages.Items.Add(e.Message);
            Invoke((MethodInvoker)(() => lstMessages.Items.Add(e.Message.Remove(e.Message.Length - 7))));
        }

        private void btnSend_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                btnEnviar_Click(sender, e);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void lblStatus_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblFechaHora.Text = DateTime.Now.ToString();
        }

        private void txtMessage_TextChanged(object sender, EventArgs e)
        {
            if ((txtMessage.Text.Length) == 1)
            {
                txtMessage.Text = txtMessage.Text[0].ToString().ToUpper();
                txtMessage.Select(2, 1);

            }
        }
    }

    #region clase server
    public class Client
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;

        public event EventHandler<MessageEventArgs> MessageReceived;

        public void Connect(string serverIP, int serverPort)
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
        }

        public void SendMessage(string message, LogicalVectorClock clock)
        {
            // Crea el mensaje con el contenido y el reloj vectorial lógico
            Message msg = new Message(message, clock);

            // Convierte el mensaje a una cadena de bytes para enviarlo a través de la red
            byte[] data = Encoding.ASCII.GetBytes(msg.ToString());

            // Envía los datos al servidor
            stream.Write(data, 0, data.Length);
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Parsea los datos recibidos y extrae el mensaje y el reloj vectorial lógico
                    Message msg = Message.Parse(receivedData);
                    LogicalVectorClock clock = msg.Clock;

                    // Dispara el evento MessageReceived para notificar al formulario del cliente
                    OnMessageReceived(msg.Content, clock);
                }
                catch (Exception ex)
                {
                    // ocurrió un error durante la recepción de mensajes
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
        private int[] vector;

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