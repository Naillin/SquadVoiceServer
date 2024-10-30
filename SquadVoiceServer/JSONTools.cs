using SquadVoiceServer.Data_Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//JSON
using System.Text.Json;
using System.IO;

namespace SquadVoiceServer
{
	internal class JSONTools
	{
		private const string FilePathUser = "users.json";
		private const string FilePathStructure = "structure.json";
		public void SaveUsers(UserDatabase userDatabase)
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			string json = JsonSerializer.Serialize(userDatabase, options);
			File.WriteAllText(FilePathUser, json); // Сохраняем список пользователей в файл
		}

		public UserDatabase LoadUsers()
		{
			if (!File.Exists(FilePathUser))
			{
				return new UserDatabase(); // Если файла нет, возвращаем пустую базу
			}

			string json = File.ReadAllText(FilePathUser); // Чтение из файла
			return JsonSerializer.Deserialize<UserDatabase>(json);
		}

		public User FindUserByUsername(string username, UserDatabase userDatabase)
		{
			return userDatabase.Users.Find(user => user.Username == username); // Поиск пользователя по имени
		}

		public User FindUserByID(int ID, UserDatabase userDatabase)
		{
			return userDatabase.Users.Find(user => user.ID == ID); // Поиск пользователя по ID
		}

		public string[] GetNameUsers(UserDatabase userDatabase)
		{
			return userDatabase.Users
							   .Select(user => user.Username) // Извлечение имени пользователей
							   .ToArray();                    // Преобразование в массив
		}

		public int GetNextAvailableId(UserDatabase userDatabase)
		{
			if (userDatabase.Users.Count == 0)
			{
				return 0; // Если нет пользователей, возвращаем 0
			}

			return userDatabase.Users.Max(user => user.ID) + 1; // Возвращаем максимальный ID + 1
		}

		public void SaveStructure(Root root)
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			string json = JsonSerializer.Serialize(root, options);
			File.WriteAllText(FilePathStructure, json); // Сохраняем в файл
		}

		public Root LoadStructure()
		{
			string json = File.ReadAllText(FilePathStructure); // Чтение из файла
			return JsonSerializer.Deserialize<Root>(json);
		}
	}
}
