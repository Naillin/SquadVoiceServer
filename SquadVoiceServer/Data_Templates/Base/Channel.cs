using System.Collections.Generic;
using System.Linq;

namespace SquadVoiceServer.Data_Templates
{
	public class Channel
	{
		public string Name { get; set; }
		public List<string> Chat { get; set; } = new List<string>();
		//public List<Voice> Voices { get; set; } = new List<Voice>();
		public List<CustomClient> ConnectedUsers { get; set; } = new List<CustomClient>();

		public bool AddUser(CustomClient user)
		{
			// Проверяем, есть ли пользователь с таким же ID
			CustomClient existingUser = ConnectedUsers.FirstOrDefault(u => u.ID == user.ID);

			if (existingUser != null)
			{
				// Удаляем пользователя с таким же ID
				existingUser.Close();
				ConnectedUsers.Remove(existingUser);
			}

			// Добавляем нового пользователя
			ConnectedUsers.Add(user);
			return true;
		}

		public bool RemoveUser(CustomClient user)
		{
			user.Close();
			return ConnectedUsers.Remove(user);
		}
	}
}
