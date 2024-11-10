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
		private string filePathRoot_ = "root.json";
		private string filePathUser_ = "users.json";
		internal JSONTools(string filePathRoot = "root.json", string filePathUser = "users.json")
		{
			filePathRoot_ = filePathRoot;
			filePathUser_ = filePathUser;
		}

		private Root root_;
		public void SaveRoot(Root root)
		{
			// Создаем глубокую копию объекта Root
			Root rootCopy = new Root
			{
				Name = root.Name,
				Channels = new List<Channel>()
			};

			// Копируем каналы и обнуляем ConnectedUsers
			foreach (Channel channel in root.Channels)
			{
				rootCopy.Channels.Add(new Channel
				{
					Name = channel.Name,
					Chat = new List<string>(channel.Chat),
					ConnectedUsers = new List<CustomClient>() // Пустой список для ConnectedUsers
				});
			}

			var options = new JsonSerializerOptions { WriteIndented = true };
			string json = JsonSerializer.Serialize(rootCopy, options);
			File.WriteAllText(filePathRoot_, json); // Сохраняем в файл
		}

		public Root LoadRoot()
		{
			string json = File.ReadAllText(filePathRoot_);
			root_ = JsonSerializer.Deserialize<Root>(json);
			return root_;
		}

		public void SaveChannel(Channel updatedChannel, Root root)
		{
			// Находим индекс канала, который нужно обновить
			int index = root.Channels.FindIndex(channel => channel.Name == updatedChannel.Name);
			if (index != -1)
			{
				// Обновляем канал, исключая данные ConnectedUsers
				root.Channels[index] = new Channel
				{
					Name = updatedChannel.Name,
					Chat = new List<string>(updatedChannel.Chat),
					ConnectedUsers = new List<CustomClient>() // Пустой список для ConnectedUsers
				};

				// Сохраняем обновленную структуру Root в JSON-файл
				SaveRoot(root);
			}
		}


		//////////////////////////////////////	USERS METHODS	//////////////////////////////////////
		private UserDatabase userDatabase_;
		public void SaveUsers(UserDatabase userDatabase)
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			string json = JsonSerializer.Serialize(userDatabase, options);
			File.WriteAllText(filePathUser_, json); // Сохраняем список пользователей в файл
		}

		public UserDatabase LoadUsers()
		{
			if (!File.Exists(filePathUser_))
			{
				return new UserDatabase(); // Если файла нет, возвращаем пустую базу
			}

			string json = File.ReadAllText(filePathUser_); // Чтение из файла
			userDatabase_ = JsonSerializer.Deserialize<UserDatabase>(json);
			return userDatabase_;
		}

		public User FindUserByUsername(string username, UserDatabase userDatabase = null)
		{
			if (userDatabase == null) { userDatabase = userDatabase_; }
			return userDatabase.Users.Find(user => user.Username == username); // Поиск пользователя по имени
		}

		public User FindUserByID(int ID, UserDatabase userDatabase = null)
		{
			if (userDatabase == null) { userDatabase = userDatabase_; }
			return userDatabase.Users.Find(user => user.ID == ID); // Поиск пользователя по ID
		}

		public string[] GetNameUsers(UserDatabase userDatabase = null)
		{
			if (userDatabase == null) { userDatabase = userDatabase_; }
			return userDatabase.Users
							   .Select(user => user.Username) // Извлечение имени пользователей
							   .ToArray();                    // Преобразование в массив
		}

		public int GetNextAvailableId(UserDatabase userDatabase = null)
		{
			if (userDatabase == null) { userDatabase = userDatabase_; }
			if (userDatabase.Users.Count == 0)
			{
				return 0; // Если нет пользователей, возвращаем 0
			}

			return userDatabase.Users.Max(user => user.ID) + 1; // Возвращаем максимальный ID + 1
		}
	}
}
