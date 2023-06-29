using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace Logic_GC
{
    public class Server
    {
        private TcpListener listener;
        private List<TcpClient> clients;
        private LogicalVectorClock clock;

        public Server()
        {
            clients = new List<TcpClient>();
            clock = new LogicalVectorClock(clients.Count);
        }

        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Servidor iniciado en el puerto " + port);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);

                // Manejar la conexión del cliente en un hilo separado
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();
            BinaryFormatter formatter = new BinaryFormatter();

            while (true)
            {
                // Leer el mensaje del cliente
                object receivedMessage = formatter.Deserialize(stream);

                // Actualizar el reloj vectorial lógico del servidor
                clock.Tick(clients.IndexOf(client));
                LogicalVectorClock clientClock = (LogicalVectorClock)receivedMessage;
                clock.Update(clientClock);

                // Procesar el mensaje del cliente
                // ...

                // Enviar una respuesta al cliente
                formatter.Serialize(stream, clock);
                stream.Flush();
            }
        }
    }

}
