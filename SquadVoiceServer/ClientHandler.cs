using NAudio.Wave;
using SquadVoiceServer.Data_Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SquadVoiceServer
{
	internal class ClientHandler
	{
		private CustomClient customClient;
		private Channel channel;
		private WaveInEvent waveSource;

		public ClientHandler(CustomClient customClient, Channel channel)
		{
			this.customClient = customClient;
			this.channel = channel;
			lock (channel.ConnectedUsers) // Блокируем доступ к списку ConnectedUsers
			{
				channel.ConnectedUsers.Add(customClient); // Добавляем пользователя в канал
			}
		}

		public void StartHandling()
		{
			//Task.Run(() => HandleTechDisconnect());
			// Обрабатываем сообщения от клиента (текст и аудио)
			Task.Run(() => HandleAudio());
			Task.Run(() => HandleChat());
			
			Task.Run(() => HandleTechConnectedClients());
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

		private void HandleTechConnectedClients()
		{
			try
			{
				NetworkTools networkTools = new NetworkTools(customClient.techClient);
				JSONTools jsonTools = new JSONTools();
				UserDatabase userDatabase = jsonTools.LoadUsers();
				while (true)
				{
					int[] connectedClientsID;
					lock (channel.ConnectedUsers) // Блокируем доступ к списку ConnectedUsers
					{
						// Получаем ID подключенных клиентов
						connectedClientsID = channel.ConnectedUsers.ToList().Select(user => user.ID).ToArray();
					}

					// Получаем имена пользователей, соответствующие ID подключенных клиентов
					var connectedUsernames = connectedClientsID
						.Select(id => userDatabase.Users.FirstOrDefault(u => u.ID == id)?.Username)
						.Where(username => username != null) // Фильтруем null значения
						.ToList();

					// Объединяем имена в одну строку, разделяя их точкой с запятой
					string connectedUsernamesString = string.Join(";", connectedUsernames);

					// Отправляем строку с именами
					networkTools.SendString(connectedUsernamesString);
					Thread.Sleep(2000);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Техническая ошибка: {ex.Message}.");
			}
		}

		private List<byte> audioBuffer = new List<byte>(); // Буфер для аудиофреймов
		private Timer sendTimer; // Таймер для отправки фреймов
		private void HandleAudio()
		{
			waveSource = new WaveInEvent();
			waveSource.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, моно

			sendTimer = new Timer(SendBufferedAudio, null, 0, 50);

			waveSource.DataAvailable += (sender, e) =>
			{
				lock (audioBuffer) // Блокировка для потокобезопасности
				{
					audioBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));
				}
			};
			waveSource.StartRecording();
		}

		// Метод для отправки данных с проверкой актуальности списка
		private void SendBufferedAudio(object state)
		{
			if (audioBuffer.Count > 0)
			{
				byte[] audioFrame;
				lock (audioBuffer) // Блокировка для потокобезопасности
				{
					if (audioBuffer.Count > 0)
					{
						audioFrame = audioBuffer.ToArray();
						audioBuffer.Clear();
					}
					else
					{
						return; // Если нет данных, выходим
					}
				}

				// Рассылаем сообщение всем в канале
				List<CustomClient> clientsToNotify;

				// Блокируем список при доступе к нему
				lock (channel.ConnectedUsers)
				{
					clientsToNotify = channel.ConnectedUsers
						.Where(otherClient => otherClient.ID != customClient.ID)
						.ToList(); // Получаем список клиентов для рассылки
				}

				foreach (CustomClient otherClient in clientsToNotify)
				{
					NetworkStream stream = otherClient.voiceClient.GetStream();
					try
					{
						stream.Write(audioFrame, 0, audioFrame.Length);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Ошибка при отправке аудио: {ex.Message}");
					}
					finally
					{
						StopHandling(); // Останавливаем обработку при завершении работы с клиентом
					}
				}
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
						lock (channel.ConnectedUsers)
						{
							clientsToNotify = channel.ConnectedUsers
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
							}
						}
					}
					else
					{
						break; // Если сообщение пустое, выходим из цикла
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
			waveSource?.StopRecording();
			waveSource?.Dispose();

			// Удаляем клиента из списка подключенных пользователей
			channel.ConnectedUsers.Remove(customClient);

			////Уничтожаем сетевые потоки
			//customClient.techClient?.GetStream()?.Close();
			//customClient.chatClient?.GetStream()?.Close();
			//customClient.voiceClient?.GetStream()?.Close();
			//customClient.videoClient?.GetStream()?.Close();
			//customClient.deskClient?.GetStream()?.Close();

			////Освобождаем ресрурсы соединения с клиентом
			//customClient.techClient?.Dispose();
			//customClient.chatClient?.Dispose();
			//customClient.voiceClient?.Dispose();
			//customClient.videoClient?.Dispose();
			//customClient.deskClient?.Dispose();

			////Закрываем соединение с клиентом
			//customClient.techClient?.Close();
			//customClient.chatClient?.Close();
			//customClient.voiceClient?.Close();
			//customClient.videoClient?.Close();
			//customClient.deskClient?.Close();

			Console.WriteLine($"Соединение с клиентом(ID: {customClient.ID.ToString()}, IP: {customClient.IP.ToString()}) закрыто.");
		}
	}
}
