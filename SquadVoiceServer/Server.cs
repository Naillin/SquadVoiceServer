using SquadVoiceServer.Data_Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SquadVoiceServer
{
	internal class Server
	{
		private static TcpListener listener;
		private int PORT_TECH = 5555; // открыть порты в маршрутизаторе
		private int PORT_CHAT = 5656;
		private int PORT_VOICE = 5757;
		private int PORT_VIDEO = 5858;
		private int PORT_DESK = 5959;
		private static List<Channel> channels = new List<Channel>(); // Список всех каналов

		public Server() 
		{
			
		}

		public void Start()
		{
			listener = new TcpListener(IPAddress.Any, PORT_TECH);
			listener.Start();
			Console.WriteLine("Server started on port " + PORT_TECH);

			// Добавляем пример канала
			channels.Add(new Channel { Name = "General" });
			channels.Add(new Channel { Name = "Gaming" });

			while (true)
			{
				TcpClient client = listener.AcceptTcpClient();
				Task.Run(() => HandleClient(client)); // Обрабатываем клиента в отдельном потоке
			}
		}

		private async void HandleClient(TcpClient client)
		{
			NetworkTools networkTools = new NetworkTools(client.GetStream());
			if (Authorization(client)) { await networkTools.SendByteAsync((byte)1); } else { await networkTools.SendByteAsync((byte)0); return; }

			// Получаем IP клиента
			IPAddress clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
			Console.WriteLine($"Клиент авторизован с IP: {clientIP.ToString()}");

			Console.WriteLine("Ожидание данных от клиента...");
			await networkTools.TakeBytesAsync();
			string channelName = networkTools.GetString();
			Console.WriteLine($"Получено имя канала: {channelName}");

			// Пример простого выбора канала
			Channel selectedChannel = channels.FirstOrDefault(c => c.Name == channelName);
			if (selectedChannel != null)
			{
				Console.WriteLine($"Клиент {clientIP.ToString()} обслуживается.");

				TcpListener listenerChat = new TcpListener(clientIP, PORT_CHAT);
				TcpListener listenerVoice = new TcpListener(clientIP, PORT_VOICE);
				TcpListener listenerVideo = new TcpListener(clientIP, PORT_VIDEO);
				TcpListener listenerDesk = new TcpListener(clientIP, PORT_DESK);

				listenerChat.Start();
				listenerVoice.Start();
				listenerVideo.Start();
				listenerDesk.Start();

				// Ожидание подключений
				TcpClient chatClient = await listenerChat.AcceptTcpClientAsync();
				TcpClient voiceClient = await listenerVoice.AcceptTcpClientAsync();
				TcpClient videoClient = await listenerVideo.AcceptTcpClientAsync();
				TcpClient deskClient = await listenerDesk.AcceptTcpClientAsync();

				ClientHandler clientHandler = new ClientHandler(client, selectedChannel); //переписать этот класс и принять всех клиентов.
				clientHandler.StartHandling();

				// Закрываем слушатели, так как клиенты уже приняты - проверить закрываются ли они в итоге и доходит до них код.
				listenerChat.Stop();
				listenerVoice.Stop();
				listenerVideo.Stop();
				listenerDesk.Stop();
			}
		}

		private bool Authorization(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			NetworkTools networkTools = new NetworkTools(stream);
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
				Console.WriteLine($"Вход одобрен {user.Username}"); return true;
			}
			else
			{
				Console.WriteLine("Вход отклонен"); return false;
			}
		}

		string filePathConfig = "configServer.txt";
		public void Initialization()
		{
			if (File.Exists(filePathConfig))
			{
				string[] linesConfig = File.ReadAllLines(filePathConfig);
				PORT_TECH = Convert.ToInt32(linesConfig[0].Split('=')[1]);  // Получаем значение port
			}
			else
			{
				File.WriteAllText(filePathConfig, $"port={PORT_TECH}");
			}
		}
	}
}
