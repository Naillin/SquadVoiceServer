using SquadVoiceServer.Data_Templates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SquadVoiceServer
{
	internal class ClientHandler
	{
		private CustomClient customClient;
		private List<Channel> channels;
		private Channel selectedChannel;

		public ClientHandler(CustomClient customClient, List<Channel> channels)
		{
			this.customClient = customClient;
			this.channels = channels;
		}

		public void StartHandling()
		{
			TakeChannelName();

			// Обрабатываем сообщения от клиента (текст и аудио)
			Task.Run(() => HandleAudio());
			Task.Run(() => HandleChat());
			
			Task.Run(() => HandleTechConnectedClients());
			Task.Run(() => HandleTechDisconnect());
			Task.Run(() => HandleTechChannel());
		}

		private void TakeChannelName()
		{
			NetworkTools networkTools = new NetworkTools(customClient.techClient);
			Console.WriteLine($"Ожидание данных от клиента {customClient.ID}...");
			string channelName = networkTools.TakeBytes().GetString();
			Console.WriteLine($"Получено имя канала: {channelName}. Клиент {customClient.IP.ToString()} обслуживается.");

			// Пример простого выбора канала
			selectedChannel = channels.FirstOrDefault(c => c.Name == channelName);
			lock (selectedChannel.ConnectedUsers) // Блокируем доступ к списку ConnectedUsers
			{
				selectedChannel.ConnectedUsers.Add(customClient); // Добавляем пользователя в канал
			}
		}

		private string channelChangeCode = "ChannelChange";
		private void HandleTechChannel()
		{
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = cancellationTokenSource.Token;
			NetworkTools networkTools = new NetworkTools(customClient.techClient);
			try
			{
				networkTools.OperationByCode(channelChangeCode, () =>
				{
					// Удаляем клиента из списка подключенных пользователей
					lock (selectedChannel.ConnectedUsers) // Блокируем доступ к списку ConnectedUsers
					{
						selectedChannel.ConnectedUsers.Remove(customClient);
					}
					StartHandling();
				}, token, true);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Техническая ошибка: {ex.Message}.");
				cancellationTokenSource.Cancel();
			}
			
		}

		private void HandleTechDisconnect()
		{
			NetworkTools networkTools = new NetworkTools(customClient.techClient);
			try
			{
				networkTools.AcceptDisconnect(customClient);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Техническая ошибка: {ex.Message}.");
			}
		}

		private void HandleTechConnectedClients() //переделать, что бы посылать только когда ConnectedUsers изменяется
		{
			try
			{
				NetworkTools networkTools = new NetworkTools(customClient.techClient);
				JSONTools jsonTools = new JSONTools();
				UserDatabase userDatabase = jsonTools.LoadUsers();

				// Храним предыдущий список ID подключенных клиентов
				HashSet<int> previousConnectedClientsID = new HashSet<int>();
				while (true)
				{
					int[] connectedClientsID;
					lock (selectedChannel.ConnectedUsers) // Блокируем доступ к списку ConnectedUsers
					{
						connectedClientsID = selectedChannel.ConnectedUsers.Select(user => user.ID).ToArray();
					}

					// Создаём текущий набор ID для сравнения
					HashSet<int> currentConnectedClientsID = new HashSet<int>(connectedClientsID);
					// Проверяем, изменился ли список
					if (!currentConnectedClientsID.SetEquals(previousConnectedClientsID))
					{
						// Сохраняем текущий список как предыдущий
						previousConnectedClientsID = currentConnectedClientsID;
						// Получаем имена пользователей, соответствующие ID подключенных клиентов
						var connectedUsernames = connectedClientsID
							.Select(id => userDatabase.Users.FirstOrDefault(u => u.ID == id)?.Username)
							.Where(username => username != null) // Фильтруем null значения
							.ToList();

						// Объединяем имена в одну строку, разделяя их точкой с запятой
						string connectedUsernamesString = string.Join(";", connectedUsernames);
						// Отправляем строку с именами
						networkTools.SendString(connectedUsernamesString);
					}
					Thread.Sleep(2000); // Проверяем изменения каждые 2 секунды
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Техническая ошибка: {ex.Message}.");
			}
		}

		//private List<byte> audioBuffer = new List<byte>(); // Буфер для аудиофреймов
		//private Timer sendTimer; // Таймер для отправки фреймов
		//private void HandleAudio1()
		//{
		//	waveSource = new WaveInEvent();
		//	waveSource.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, моно

		//	sendTimer = new Timer(SendBufferedAudio, null, 0, 100);

		//	waveSource.DataAvailable += (sender, e) =>
		//	{
		//		audioBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));
		//	};
		//	waveSource.StartRecording();
		//}

		//// Метод для отправки данных с проверкой актуальности списка
		//private void SendBufferedAudio(object state)
		//{
		//	if (audioBuffer.Count > 0)
		//	{
		//		byte[] audioFrame;
		//		if (audioBuffer.Count > 0)
		//		{
		//			audioFrame = audioBuffer.ToArray();
		//			audioBuffer.Clear();
		//		}
		//		else
		//		{
		//			return;
		//		}

		//		List<CustomClient> clientsToNotify;

		//		lock (channel.ConnectedUsers)
		//		{
		//			clientsToNotify = channel.ConnectedUsers
		//				.Where(otherClient => otherClient.ID != customClient.ID)
		//				.ToList();
		//		}

		//		foreach (CustomClient otherClient in clientsToNotify)
		//		{
		//			NetworkStream stream = otherClient.voiceClient.GetStream();
		//			try
		//			{
		//				Console.WriteLine($"Отправляем аудио клиенту ID: {otherClient.ID}");
		//				stream.Write(audioFrame, 0, audioFrame.Length);
		//			}
		//			catch (Exception ex)
		//			{
		//				Console.WriteLine($"Ошибка при отправке аудио клиенту {otherClient.ID}: {ex.Message}");
		//			}
		//		}
		//	}
		//}

		private void HandleAudio()
		{
			try
			{
				NetworkTools networkTools = new NetworkTools(customClient.voiceClient);
				while (true)
				{
					byte[] voiceArray = networkTools.TakeBytes().GetBytes();
					if (voiceArray != null && voiceArray.Length > 0)
					{
						//Console.WriteLine($"Получено сообщение: {message}");

						// Рассылаем сообщение всем в канале
						List<CustomClient> clientsToNotify;
						// Блокируем список при доступе к нему
						lock (selectedChannel.ConnectedUsers)
						{
							clientsToNotify = selectedChannel.ConnectedUsers
								.Where(otherClient => otherClient.ID != customClient.ID)
								.ToList(); // Получаем список клиентов для рассылки
						}

						foreach (CustomClient otherClient in clientsToNotify)
						{
							try
							{
								NetworkTools networkToolsUser = new NetworkTools(otherClient.voiceClient);
								networkToolsUser.SendByte(voiceArray);
							}
							catch (Exception ex)
							{
								Console.WriteLine($"Ошибка при отправке голоса клиенту {otherClient.ID}: {ex.Message}");
								break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при обработке чата: {ex.Message}.");
			}
			finally
			{
				StopHandling(); // Останавливаем обработку при завершении работы с клиентом
			}
		}

		private void HandleChat()
		{
			try
			{
				NetworkTools networkTools = new NetworkTools(customClient.chatClient);
				while (true)
				{
					string message = networkTools.TakeBytes().GetString();
					if (!string.IsNullOrEmpty(message))
					{
						Console.WriteLine($"Получено сообщение: {message}");

						// Рассылаем сообщение всем в канале
						List<CustomClient> clientsToNotify;

						// Блокируем список при доступе к нему
						lock (selectedChannel.ConnectedUsers)
						{
							clientsToNotify = selectedChannel.ConnectedUsers
								.Where(otherClient => otherClient.ID != customClient.ID)
								.ToList(); // Получаем список клиентов для рассылки
						}

						foreach (CustomClient otherClient in clientsToNotify)
						{
							try
							{
								NetworkTools networkToolsUser = new NetworkTools(otherClient.chatClient);
								networkToolsUser.SendString(message);
							}
							catch (Exception ex)
							{
								Console.WriteLine($"Ошибка при отправке сообщения клиенту {otherClient.ID}: {ex.Message}");
								break;
							}
						}
					}
					else
					{

					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при обработке чата: {ex.Message}.");
			}
			finally
			{
				StopHandling(); // Останавливаем обработку при завершении работы с клиентом
			}
		}

		public void StopHandling()
		{
			// Останавливаем захват аудио и освобождаем ресурсы
			//waveSource?.StopRecording();
			//waveSource?.Dispose();

			// Удаляем клиента из списка подключенных пользователей
			selectedChannel.ConnectedUsers.Remove(customClient);

			customClient.Close();

			Console.WriteLine($"Соединение с клиентом(ID: {customClient.ID.ToString()}, IP: {customClient.IP.ToString()}) закрыто.");
		}
	}
}
