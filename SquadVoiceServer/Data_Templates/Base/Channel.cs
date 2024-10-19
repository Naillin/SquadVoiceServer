using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;

namespace SquadVoiceServer.Data_Templates
{
	public class Channel
	{
		public string Name { get; set; }
		public List<string> Chat { get; set; } = new List<string>();
		//public List<Voice> Voices { get; set; } = new List<Voice>();
		public List<TcpClient> ConnectedUsers { get; set; } = new List<TcpClient>();
	}
}
