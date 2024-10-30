using System.Collections.Generic;

namespace SquadVoiceServer.Data_Templates
{
	public class Channel
	{
		public string Name { get; set; }
		public List<string> Chat { get; set; } = new List<string>();
		//public List<Voice> Voices { get; set; } = new List<Voice>();
		public List<CustomClient> ConnectedUsers { get; set; } = new List<CustomClient>();
	}
}
