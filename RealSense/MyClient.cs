using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NamedPipeWrapper;

namespace NamedPipeWrapper
{
    class MyClient
    {
        /// <summary>
        /// Modified class to act as a named pipe server. Can add own function to subscribe to connection message event etc
        /// </summary>
        private NamedPipeClient<string> client;


        public MyClient(string pipeName)
        {
            this.client = new NamedPipeClient<string>(pipeName);
            client.ServerMessage += OnServerMessage;
            client.Error += OnError;
            client.Start();

        }

        private void OnServerMessage(NamedPipeConnection<string, string> connection, string message)
        {
            Console.WriteLine("Server says: {0}", message);
        }

        private void OnError(Exception exception)
        {
            Console.Error.WriteLine("ERROR: {0}", exception);
        }

        public void SendMessage(string message)
        {
            client.PushMessage(message);
        }

        public void Stop()
        {
            client.Stop();
        }
    }
}