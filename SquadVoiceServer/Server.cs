using SquadVoiceServer.Data_Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SquadVoiceServer
{
	internal class Server
	{
		private int PORT_TECH = 5555;
		private int PORT_CHAT = 5656;
		private int PORT_VOICE = 5757;
		private int PORT_VIDEO = 5858;
		private int PORT_DESK = 5959;
		static private Root root;
		UserDatabase userDatabase;

		public Server() 
		{
			
		}

		public void Start()
		{
			Console.WriteLine($"Server started on port {PORT_TECH}.");

			// Чтение корня каналов
			JSONTools jSONTools = new JSONTools();
			root = jSONTools.LoadRoot();
			userDatabase = jSONTools.LoadUsers();
			Console.WriteLine($"Loaded channels {string.Join(", ", root.Channels.Select(channel => channel.Name))}.");

			while (true)
			{
				TcpClient client = new NetworkTools().AcceptConnection(PORT_TECH);
				Task.Run(() => HandleClient(client)); // Обрабатываем клиента в отдельном потоке
			}
		}

		private void HandleClient(TcpClient client)
		{
			NetworkTools networkTools = new NetworkTools(client);
			int ID_user = Authorization(client);
			if (ID_user != -1) { networkTools.SendByte(BitConverter.GetBytes(1)); } else { networkTools.SendByte(BitConverter.GetBytes(0)); return; }

			// Получаем IP клиента
			IPAddress clientIP = networkTools.GetIP();
			Console.WriteLine($"Клиент(ID: {ID_user.ToString()}, IP: {clientIP.ToString()}) авторизован.");

			CustomClient customClient = new CustomClient(
				ID_user,
				clientIP,
				client,
				networkTools.GetClient(clientIP, PORT_CHAT),
				networkTools.GetClient(clientIP, PORT_VOICE),
				networkTools.GetClient(clientIP, PORT_VIDEO),
				networkTools.GetClient(clientIP, PORT_DESK)
			);

			Console.WriteLine("Отправка каналов клиенту...");
			string channelsNames = string.Join(";", root.Channels.Select(channel => channel.Name));
			networkTools.SendString(channelsNames);

			ClientHandler clientHandler = new ClientHandler(customClient, root, userDatabase);
			clientHandler.StartHandling();
		}

		private int Authorization(TcpClient client)
		{
			NetworkTools networkTools = new NetworkTools(client);
			string loginPass = networkTools.TakeBytes().GetString();
			Console.WriteLine($"Попытка входа: {loginPass}");

			// Простая проверка логина и пароля
			string[] parts = loginPass.Split(':');
			string login = parts[0];
			string password = parts[1];

			// Поиск пользователя
			JSONTools jsonTools = new JSONTools();
			//Загрузка базы данных пользователей
			UserDatabase userDatabase = jsonTools.LoadUsers();
			User user = jsonTools.FindUserByUsername(login, userDatabase);
			if (user != null && user.Password == password) // Условие для правильных данных
			{
				Console.WriteLine($"Вход одобрен {user.Username}."); return user.ID;
			}
			else
			{
				Console.WriteLine("Вход отклонен."); return -1;
			}
		}

		string filePathConfig = "configServer.txt";
		public void Initialization()
		{
			if (File.Exists(filePathConfig))
			{
				string[] linesConfig = File.ReadAllLines(filePathConfig);
				PORT_TECH = Convert.ToInt32(linesConfig[0].Split('=')[1]);
				PORT_CHAT = Convert.ToInt32(linesConfig[1].Split('=')[1]);
				PORT_VOICE = Convert.ToInt32(linesConfig[2].Split('=')[1]);
				PORT_VIDEO = Convert.ToInt32(linesConfig[3].Split('=')[1]);
				PORT_DESK = Convert.ToInt32(linesConfig[4].Split('=')[1]);
			}
			else
			{
				string configTextDefault = $"port_tech=5555\r\n" +
										   $"port_chat=5656\r\n" +
										   $"port_voice=5757\r\n" +
										   $"port_video=5858\r\n" +
										   $"port_desk=5959";
				File.WriteAllText(filePathConfig, configTextDefault);
			}
		}
	}
}
