using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquadVoiceServer.Data_Templates
{
	public class Root
	{
		public string Name { get; set; }
		public List<Channel> Channels { get; set; }
	}
}
