using System;
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
		List<Channel> channels = new List<Channel>(); // Список всех каналов

		public static void Main(string[] args)
		{
			Program program = new Program();
			program.init();

			listener = new TcpListener(IPAddress.Any, SERVER_PORT);
			listener.Start();
			Console.WriteLine("Server started on port " + SERVER_PORT);

			// Добавляем пример канала
			program.channels.Add(new Channel { Name = "General" });
			program.channels.Add(new Channel { Name = "Gaming" });
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
			//АВТОРИЗАЦИЯ
			//Загрузка базы данных пользователей
			JSONTools jsonTools = new JSONTools();
			UserDatabase userDatabase = jsonTools.LoadUsers();
			Program program = new Program();
			NetworkTools networkTools = new NetworkTools();

			using (NetworkStream stream = client.GetStream())
			{
				string loginPass = networkTools.getData(stream, 256);
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

				string channelName = networkTools.getData(stream, 256);
				// Пример простого выбора канала
				Channel selectedChannel = program.channels.FirstOrDefault(c => c.Name == channelName);
				if (selectedChannel != null)
				{
					ClientHandler clientHandler = new ClientHandler(client, selectedChannel);
					clientHandler.StartHandling();
				}
			}

			client.Close();
		}

		string filePathConfig = "configServer.txt";
		private void init()
		{
			string[] linesConfig = File.ReadAllLines(filePathConfig);
			SERVER_PORT = Convert.ToInt32(linesConfig[0].Split('=')[1]);  // Получаем значение port
		}
	}
}
