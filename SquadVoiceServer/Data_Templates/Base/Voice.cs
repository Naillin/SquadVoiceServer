using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SquadVoiceServer.Data_Templates
{
	public class Voice
	{
		public string Name { get; set; }
		public List<TcpClient> ConnectedUsers { get; set; } = new List<TcpClient>();

		public void BroadcastAudio(byte[] audioData, TcpClient sender)
		{
			foreach (var user in ConnectedUsers)
			{
				if (user != sender)
				{
					NetworkStream userStream = user.GetStream();
					userStream.Write(audioData, 0, audioData.Length);
				}
			}
		}
	}
}
