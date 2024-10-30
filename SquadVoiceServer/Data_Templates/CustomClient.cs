using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace SquadVoiceServer.Data_Templates
{
	public class CustomClient
	{
		public int ID;
		public IPAddress IP;
		public TcpClient techClient;
		public TcpClient chatClient;
		public TcpClient voiceClient;
		public TcpClient videoClient;
		public TcpClient deskClient;

		public CustomClient(int ID, IPAddress IP, TcpClient techClient, TcpClient chatClient, TcpClient voiceClient, TcpClient videoClient, TcpClient deskClient)
		{
			this.ID = ID;
			this.IP = IP;
			this.techClient = techClient;
			this.chatClient = chatClient;
			this.voiceClient = voiceClient;
			this.videoClient = videoClient;
			this.deskClient = deskClient;
		}

		public void Close()
		{
			//Уничтожаем сетевые потоки
			//this.techClient?.GetStream()?.Close();
			//this.chatClient?.GetStream()?.Close();
			//this.voiceClient?.GetStream()?.Close();
			//this.videoClient?.GetStream()?.Close();
			//this.deskClient?.GetStream()?.Close();

			//Освобождаем ресрурсы соединения с клиентом
			this.techClient?.Dispose();
			this.chatClient?.Dispose();
			this.voiceClient?.Dispose();
			this.videoClient?.Dispose();
			this.deskClient?.Dispose();

			//Закрываем соединение с клиентом
			this.techClient?.Close();
			this.chatClient?.Close();
			this.voiceClient?.Close();
			this.videoClient?.Close();
			this.deskClient?.Close();
		}
	}
}
