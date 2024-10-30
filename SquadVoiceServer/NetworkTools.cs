using SquadVoiceServer.Data_Templates;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SquadVoiceServer
{
	//////////////////////////////////////// СДЕЛАТЬ УНИЧТОЖЕНИЕ КЛИЕНТА НА TakeBytes ЕСЛИ ОН СОРВАЛ СОЕДИНЕНИЕ!!!!!!!!!!!!!
	/// <summary>
	/// Инструментарий для работы с TCP-соединениями.
	/// </summary>
	internal class NetworkTools
	{
		private TcpClient _client;
		private NetworkStream _stream;
		/// <summary>
		/// Инструментарий для работы с TCP-соединениями.
		/// </summary>
		/// <param name="client">Клиентское подключение через протокол TCP.</param>
		public NetworkTools(TcpClient client = null)
		{
			_client = client;
			_stream = _client?.GetStream();
		}

		private byte[] bufferOut;
		/// <summary>
		/// Получает байты информации от источника в сетевом потоке.
		/// </summary>
		/// <returns>Экземпляр собственного класса.</returns>
		public NetworkTools TakeBytes()
		{
			if (_stream == null)
				return this;

			List<byte> buffer = new List<byte>();
			byte[] tempBuffer = new byte[256];
			int bytesRead;

			do
			{
				bytesRead = _stream.Read(tempBuffer, 0, tempBuffer.Length);
				buffer.AddRange(tempBuffer.Take(bytesRead));
			} while (_stream.DataAvailable);

			bufferOut = buffer.ToArray();
			return this;
		}

		/// <summary>
		/// Асинхронно получает байты информации от источника в сетевом потоке.
		/// </summary>
		/// <returns>Задачу в виде экземпляра собственного класса.</returns>
		public async Task<NetworkTools> TakeBytesAsync()
		{
			if (_stream == null)
				return this;

			List<byte> buffer = new List<byte>();
			byte[] tempBuffer = new byte[256];
			int bytesRead;

			do
			{
				bytesRead = await _stream.ReadAsync(tempBuffer, 0, tempBuffer.Length);
				buffer.AddRange(tempBuffer.Take(bytesRead));
			} while (_stream.DataAvailable);

			bufferOut = buffer.ToArray();
			return this;
		}

		/// <summary>
		/// Возвращает полученные байты.
		/// </summary>
		/// <returns>Массив байтов.</returns>
		public byte[] GetBytes() { return bufferOut; }

		/// <summary>
		/// Возвращает декодированную строку из полученных байтов.
		/// </summary>
		/// <returns>Строка содержащая сообщение.</returns>
		public string GetString()
		{
			return Encoding.UTF8.GetString(bufferOut);
		}

		/// <summary>
		/// Возвращает булевое значение из полученных байтов.
		/// </summary>
		/// <returns>Значение булевого типа данных.</returns>
		public bool GetBool()
		{
			// Если первый байт 0, вернём false, иначе true
			return bufferOut.Length > 0 && bufferOut[0] != 0;
		}

		/// <summary>
		/// Возвращает целочисленное значение из полученных байтов.
		/// </summary>
		/// <returns>Целое число.</returns>
		public int GetInt()
		{
			return BitConverter.ToInt32(bufferOut, 0);
		}

		/// <summary>
		/// Возвращает вещественное значение из полученных байтов.
		/// </summary>
		/// <returns>Число с плавающей точкой.</returns>
		public double GetDouble()
		{
			return BitConverter.ToDouble(bufferOut, 0);
		}

		/// <summary>
		/// Отправляет строку сообщения в сетевой поток.
		/// </summary>
		/// <param name="data">Строка сообщения.</param>
		public void SendString(string data)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(data);
			_stream.Write(messageBytes, 0, messageBytes.Length);
		}

		/// <summary>
		/// Отправляет массив байтов в сетевой поток.
		/// </summary>
		/// <param name="data">Массив байтов.</param>
		public void SendByte(byte[] data)
		{
			_stream.Write(data, 0, data.Length);
		}

		/// <summary>
		/// Асинхронно отправляет строку сообщения в сетевой поток.
		/// </summary>
		/// <param name="data">Строка сообщения.</param>
		public async Task SendStringAsync(string data)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(data);
			await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
		}

		/// <summary>
		/// Асинхронно отправляет массив байтов в сетевой поток.
		/// </summary>
		/// <param name="data">Байты.</param>
		public async Task SendByteAsync(byte[] data)
		{
			await _stream.WriteAsync(data, 0, data.Length);
		}

		/// <summary>
		/// Применить клиента к текущему экземпляру класса.
		/// </summary>
		/// <param name="client">Применяемый клиент.</param>
		private void ApplyClient(TcpClient client)
		{
			_client = client;
			_stream = _client?.GetStream();
		}

		/// <summary>
		/// Закрыть TCP-соединение текущего клиента.
		/// </summary>
		public void CloseClient()
		{
			_client?.Dispose();
			_client?.Close();
		}

		/// <summary>
		/// Закрыть поток текущего клиента.
		/// </summary>
		public void CloseStream()
		{
			_stream?.Dispose();
			_stream?.Close();
		}

		/// <summary>
		/// Строка представляющая код для проверки соединений (по умолчанию "SquadVoice").
		/// </summary>
		static public string connectionCode
		{
			get { return _connectionCode; }
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					_connectionCode = value;
				}
			}
		}
		private static string _connectionCode = "SquadVoice";
		/// <summary>
		/// Принимает подключение клиента.
		/// </summary>
		/// <param name="port">Прослушиваемый порт.</param>
		/// <param name="apply">Применение полученного клиента к экземпляру класса (по умолчанию false).</param>
		/// <returns>Клиентское подключение через протокол TCP.</returns>
		public TcpClient AcceptConnection(int port, bool apply = false)
		{
			TcpClient result = null;
			TcpListener listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			result = listener.AcceptTcpClient();

			if (new NetworkTools(result).TakeBytes().GetString().Equals(_connectionCode))
			{
				listener.Stop();
				if (apply) ApplyClient(result);
				return result;
			}
			else { listener.Stop(); result.Close(); return null; }
		}

		/// <summary>
		/// Выполняет запрос на подключение к удаленному хосту.
		/// </summary>
		/// <param name="ip">Адрес хоста.</param>
		/// <param name="port">Порт хоста.</param>
		/// <param name="apply">Применение полученного клиента к экземпляру класса (по умолчанию false).</param>
		/// <returns>Клиентское подключение через протокол TCP.</returns>
		public TcpClient TryConnection(IPAddress ip, int port, bool apply = false)
		{
			TcpClient result = new TcpClient(ip.ToString(), port);
			new NetworkTools(result).SendString(_connectionCode);

			if (apply) ApplyClient(result);
			return result;
		}

		/// <summary>
		/// Возвращает адрес подключенного источника.
		/// </summary>
		/// <param name="client">Клиентское подключение через протокол TCP (по умолчанию null).</param>
		/// <returns>Адрес клиента экземпляра класса, в противном случае адрес указанного клиента.</returns>
		public IPAddress GetIP(TcpClient client = null)
		{
			if (client == null) { return ((IPEndPoint)_client.Client.RemoteEndPoint).Address; }
			else { return ((IPEndPoint)client.Client.RemoteEndPoint).Address; }
		}

		/// <summary>
		/// Прослушивание подключения на конкретном адресе и порте. 
		/// </summary>
		/// <param name="ip">Адрес клиента.</param>
		/// <param name="port">Порт клиента.</param>
		/// <param name="apply">Применение полученного клиента к экземпляру класса (по умолчанию false).</param>
		/// <returns>Клиентское подключение через протокол TCP.</returns>
		public TcpClient GetClient(IPAddress ip, int port, bool apply = false)
		{
			TcpClient result = null;

			while (true)
			{
				result = new NetworkTools().AcceptConnection(port);
				if (GetIP(result).Equals(ip)) { break; } // Проверяем IP-адрес
				else { result.Close(); } 

				Thread.Sleep(100);
			}

			if (apply) ApplyClient(result);
			return result;
		}

		/// <summary>
		/// Выполняет переданную операцию при получении указанного кода от источника.
		/// </summary>
		/// <param name="code">Код операции.</param>
		/// <param name="operation">Передаваемая операция.</param>
		/// <param name="cancellationToken">Токен завершающий операцию если активирован флаг цикличного выполнения.</param>
		/// <param name="cycle">Флаг активирующий цикличное выполнение операции.</param>
		public void OperationByCode(string code, Action operation, CancellationToken cancellationToken, bool cycle = false)
		{
			while (cycle && !cancellationToken.IsCancellationRequested)
			{
				string data = this.TakeBytes().GetString();
				if (data.Equals(code))
				{
					operation();
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		private string disconnectString = "Disconnect";
		public void TryDisconnect(TcpClient client)
		{
			new NetworkTools(client).SendString(disconnectString);
		}

		public void AcceptDisconnect(CustomClient customClient)
		{
			if(TakeBytes().GetString().Equals(disconnectString))
			{
				// Уничтожаем сетевые потоки
				customClient.techClient?.GetStream()?.Close();
				customClient.chatClient?.GetStream()?.Close();
				customClient.voiceClient?.GetStream()?.Close();
				customClient.videoClient?.GetStream()?.Close();
				customClient.deskClient?.GetStream()?.Close();

				// Освобождаем ресрурсы соединения с клиентом
				customClient.techClient?.Dispose();
				customClient.chatClient?.Dispose();
				customClient.voiceClient?.Dispose();
				customClient.videoClient?.Dispose();
				customClient.deskClient?.Dispose();

				// Закрываем соединение с клиентом
				customClient.techClient?.Close();
				customClient.chatClient?.Close();
				customClient.voiceClient?.Close();
				customClient.videoClient?.Close();
				customClient.deskClient?.Close();
			}
		}
	}
}
