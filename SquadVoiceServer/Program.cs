﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SquadVoiceServer.Data_Templates;
using System.IO;
using System.Runtime.Remoting.Channels;

namespace SquadVoiceServer
{
	internal class Program
	{
		private static TcpListener listener;
		private static int SERVER_PORT = 5656;
		private static List<Channel> channels = new List<Channel>(); // Список всех каналов

		public static void Main(string[] args)
		{
			Program program = new Program();
			program.init();

			listener = new TcpListener(IPAddress.Any, SERVER_PORT);
			listener.Start();
			Console.WriteLine("Server started on port " + SERVER_PORT);

			// Добавляем пример канала
			channels.Add(new Channel { Name = "General" });
			channels.Add(new Channel { Name = "Gaming" });
			//Channel generalChannel = program.channels.FirstOrDefault(c => c.Name == "General");
			//if (generalChannel != null)
			//{
			//	generalChannel.Voices.Add(new Voice { Name = "V1" });
			//	generalChannel.Voices.Add(new Voice { Name = "V2" });
			//}

			while (true)
			{
				TcpClient client = listener.AcceptTcpClient();
				Task.Run(() => HandleClient(client)); // Обрабатываем клиента в отдельном потоке
			}
		}

		private static void HandleClient(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			//АВТОРИЗАЦИЯ
			//Загрузка базы данных пользователей
			JSONTools jsonTools = new JSONTools();
			UserDatabase userDatabase = jsonTools.LoadUsers();
			NetworkTools networkTools = new NetworkTools(stream);
			Program program = new Program();

			string loginPass = networkTools.getData();
			Console.WriteLine($"Попытка входа: {loginPass}");
			// Простая проверка логина и пароля
			string[] parts = loginPass.Split(':');
			string login = parts[0];
			string password = parts[1];

			// Поиск пользователя
			User user = jsonTools.FindUserByUsername(login, userDatabase);
			if (user != null && user.Password == password) // Условие для правильных данных
			{
				Console.WriteLine("Вход одобрен");
				// Отправляем 1 (true) клиенту
				byte[] responseData = new byte[] { (byte)1 };
				stream.Write(responseData, 0, responseData.Length);
			}
			else
			{
				Console.WriteLine("Вход отклонен");
				// Отправляем 0 (false) клиенту
				byte[] responseData = new byte[] { (byte)0 };
				stream.Write(responseData, 0, responseData.Length);

				return;
			}

			Console.WriteLine("Ожидание данных от клиента...");
			string channelName = networkTools.getData();
			Console.WriteLine($"Получено имя канала: {channelName}");
			// Пример простого выбора канала
			Channel selectedChannel = channels.FirstOrDefault(c => c.Name == channelName);
			if (selectedChannel != null)
			{
				Console.WriteLine($"Клиент {user.Username} обслуживается.");
				ClientHandler clientHandler = new ClientHandler(client, selectedChannel);
				clientHandler.StartHandling();
			}
		}

		string filePathConfig = "configServer.txt";
		private void init()
		{
			if (File.Exists(filePathConfig))
			{
				string[] linesConfig = File.ReadAllLines(filePathConfig);
				SERVER_PORT = Convert.ToInt32(linesConfig[0].Split('=')[1]);  // Получаем значение port
			}
			else
			{
				File.WriteAllText(filePathConfig, $"port={SERVER_PORT}");
			}
		}
	}
}
