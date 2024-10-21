using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SquadVoiceServer.Data_Templates;
using System.IO;
using System.Runtime.Remoting.Channels;

namespace SquadVoiceServer
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			Server server = new Server();
			server.Initialization();
			server.Start();
		}
	}
}
