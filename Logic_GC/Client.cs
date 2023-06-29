using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace Logic_GC
{
    public class Client
    {
        private TcpClient client;
        private LogicalVectorClock clock;

        public Client()
        {
            clock = new LogicalVectorClock(1);
        }

        public void Connect(string serverIP, int serverPort)
        {
            client = new TcpClient(serverIP, serverPort);
            NetworkStream stream = client.GetStream();
            BinaryFormatter formatter = new BinaryFormatter();

            while (true)
            {
                // Enviar el reloj vectorial lógico al servidor
                formatter.Serialize(stream, clock);
                stream.Flush();

                // Recibir la respuesta del servidor
                object receivedMessage = formatter.Deserialize(stream);
                LogicalVectorClock serverClock = (LogicalVectorClock)receivedMessage;

                // Actualizar el reloj vectorial lógico del cliente
                clock.Tick(0);
                clock.Update(serverClock);

                // Procesar la respuesta del servidor
                // ...

                // Esperar un tiempo antes de enviar el siguiente mensaje
                Thread.Sleep(1000);
            }
        }
    }

}
