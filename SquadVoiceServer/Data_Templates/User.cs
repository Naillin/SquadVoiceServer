﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquadVoiceServer.Data_Templates
{
	public class User
	{
		public int ID { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public int Status { get; set; }
	}
}
