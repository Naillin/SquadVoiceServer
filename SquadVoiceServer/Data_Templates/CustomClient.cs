using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace SquadVoiceServer.Data_Templates
{
	public struct CustomClient
	{
		public int ID;
		public IPAddress IP;
		public TcpClient techClient;
		public TcpClient chatClient;
		public TcpClient voiceClient;
		public TcpClient videoClient;
		public TcpClient deskClient;

		CustomClient(int ID, IPAddress IP, TcpClient techClient, TcpClient chatClient, TcpClient voiceClient, TcpClient videoClient, TcpClient deskClient)
		{
			this.ID = ID;
			this.IP = IP;
			this.techClient = techClient;
			this.chatClient = chatClient;
			this.voiceClient = voiceClient;
			this.videoClient = videoClient;
			this.deskClient = deskClient;
		}
	}
}
