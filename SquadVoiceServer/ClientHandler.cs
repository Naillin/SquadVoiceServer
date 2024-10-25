using NAudio.Wave;
using SquadVoiceServer.Data_Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
			channel.ConnectedUsers.Add(customClient); // Добавляем пользователя в канал
		}

		public void StartHandling()
		{
			Task.Run(() => HandleTech());
			// Обрабатываем сообщения от клиента (текст и аудио)
			Task.Run(() => HandleAudio());
			Task.Run(() => HandleChat());
		}

		private void HandleTech()
		{
			NetworkTools networkTools = new NetworkTools(customClient.techClient);
			try
			{
				networkTools.AcceptDisconnect(customClient);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Техническаыя ошибка: {ex.Message}.");
			}
		}

		private void HandleAudio()
		{
			try
			{
				waveSource = new WaveInEvent();
				waveSource.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, моно
				waveSource.DataAvailable += (sender, e) =>
				{
					// Рассылаем аудио всем пользователям в канале
					foreach (CustomClient otherClients in channel.ConnectedUsers)
					{
						TcpClient otherVoiceClient = otherClients.voiceClient;
						if (otherVoiceClient != customClient.voiceClient) // Не отправляем аудио самому себе
						{
							NetworkStream stream = otherVoiceClient.GetStream();
							stream.Write(e.Buffer, 0, e.BytesRecorded);
						}
					}
				};
				waveSource.StartRecording();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при обработке аудио: {ex.Message}.");
				StopHandling(); // Останавливаем обработку при ошибке
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
						foreach (CustomClient otherClients in channel.ConnectedUsers)
						{
							TcpClient otherChatClient = otherClients.chatClient;
							if (otherChatClient != customClient.chatClient)
							{
								NetworkTools networkToolsUser = new NetworkTools(otherChatClient);
								networkToolsUser.SendString(message);
							}
						}
					}
					else
					{
						break;
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
