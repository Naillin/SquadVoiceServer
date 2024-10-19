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
		private NetworkStream _stream;

		public NetworkTools(NetworkStream stream)
		{
			_stream = stream;
		}
		public string getData()
		{
			List<byte> buffer = new List<byte>();
			byte[] tempBuffer = new byte[256];
			int bytesRead;

			do
			{
				bytesRead = _stream.Read(tempBuffer, 0, tempBuffer.Length);
				buffer.AddRange(tempBuffer.Take(bytesRead));
			}
			while (_stream.DataAvailable);

			return Encoding.UTF8.GetString(buffer.ToArray());
		}

		public void sendData(string data)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(data);
			_stream.Write(messageBytes, 0, messageBytes.Length);
		}
	}
}
