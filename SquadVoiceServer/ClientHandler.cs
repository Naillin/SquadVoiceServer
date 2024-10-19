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
		private TcpClient client;
		private Channel channel;
		private WaveInEvent waveSource;

		public ClientHandler(TcpClient client, Channel channel)
		{
			this.client = client;
			this.channel = channel;
			channel.ConnectedUsers.Add(client); // Добавляем пользователя в канал
		}

		public void StartHandling()
		{
			// Обрабатываем сообщения от клиента (текст и аудио)
			Task.Run(() => HandleAudio());
			//Task.Run(() => HandleChat());
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
					foreach (var user in channel.ConnectedUsers)
					{
						if (user != client) // Не отправляем аудио самому себе
						{
							NetworkStream stream = user.GetStream();
							stream.Write(e.Buffer, 0, e.BytesRecorded);
						}
					}
				};
				waveSource.StartRecording();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при обработке аудио: {ex.Message}");
				StopHandling(); // Останавливаем обработку при ошибке
			}
		}

		private void HandleChat()
		{
			try
			{
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[1024];
				while (true)
				{
					int bytesRead = stream.Read(buffer, 0, buffer.Length);
					if (bytesRead == 0)
					{
						// Клиент отключился
						Console.WriteLine("Клиент отключился");
						break;
					}

					string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
					Console.WriteLine($"Получено сообщение: {message}");

					// Рассылаем сообщение всем в канале
					foreach (var user in channel.ConnectedUsers)
					{
						if (user != client)
						{
							NetworkStream userStream = user.GetStream();
							byte[] data = Encoding.UTF8.GetBytes(message);
							userStream.Write(data, 0, data.Length);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при обработке чата: {ex.Message}");
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
			channel.ConnectedUsers.Remove(client);

			// Закрываем соединение с клиентом
			client.Close();
			Console.WriteLine("Соединение с клиентом закрыто.");
		}
	}

}
