using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SquadVoiceServer
{
	internal class NetworkTools
	{
		public string getData(NetworkStream stream, int bufferSize)
		{
			byte[] buffer = new byte[bufferSize];
			int bytesRead = stream.Read(buffer, 0, buffer.Length);
			string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			return message;
		}

		public void sendData(NetworkStream stream, string data)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(data);
			stream.Write(messageBytes, 0, messageBytes.Length);
		}
	}
}
