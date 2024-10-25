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
	internal class NetworkTools
	{
		private TcpClient _client;
		private NetworkStream _stream;
		public NetworkTools(TcpClient client = null)
		{
			_client = client;
			_stream = _client?.GetStream();
		}

		private byte[] bufferOut;
		public NetworkTools TakeBytes()
		{
			List<byte> buffer = new List<byte>();
			byte[] tempBuffer = new byte[256];
			int bytesRead;

			do
			{
				bytesRead = _stream.Read(tempBuffer, 0, tempBuffer.Length);
				buffer.AddRange(tempBuffer.Take(bytesRead));
			}
			while (_stream.DataAvailable);

			bufferOut = buffer.ToArray();
			return this;
		}

		// Асинхронный метод для принятия пакетов
		public async Task<NetworkTools> TakeBytesAsync()
		{
			List<byte> buffer = new List<byte>();
			byte[] tempBuffer = new byte[256];
			int bytesRead;

			do
			{
				bytesRead = await _stream.ReadAsync(tempBuffer, 0, tempBuffer.Length);
				buffer.AddRange(tempBuffer.Take(bytesRead));
			}
			while (_stream.DataAvailable);

			bufferOut = buffer.ToArray();
			return this;
		}

		public byte[] GetBytes() { return bufferOut; }

		public string GetString()
		{
			return Encoding.UTF8.GetString(bufferOut);
		}

		public bool GetBool()
		{
			// Если первый байт 0, вернём false, иначе true
			return bufferOut.Length > 0 && bufferOut[0] != 0;
		}

		public int GetInt()
		{
			return BitConverter.ToInt32(bufferOut, 0);
		}

		public double GetDouble()
		{
			return BitConverter.ToDouble(bufferOut, 0);
		}

		public void SendString(string data)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(data);
			_stream.Write(messageBytes, 0, messageBytes.Length);
		}

		public void SendByte(byte data)
		{
			byte[] responseData = new byte[] { data };
			_stream.Write(responseData, 0, responseData.Length);
		}

		// Асинхронный метод для отправки строки
		public async Task SendStringAsync(string data)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(data);
			await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
		}

		public async Task SendByteAsync(byte data)
		{
			byte[] responseData = new byte[] { data };
			await _stream.WriteAsync(responseData, 0, responseData.Length);
		}

		private void ApplyClient(TcpClient client)
		{
			_client = client;
			_stream = _client?.GetStream();
		}

		// Метод для закрытия клиента
		public void CloseClient()
		{
			_client?.Dispose();
			_client?.Close();
		}

		// Метод для закрытия потока
		public void CloseStream()
		{
			_stream?.Dispose();
			_stream?.Close();
		}

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

		public TcpClient TryConnection(IPAddress ip, int port, bool apply = false)
		{
			TcpClient result = new TcpClient(ip.ToString(), port);
			new NetworkTools(result).SendString(_connectionCode);

			if (apply) ApplyClient(result);
			return result;
		}

		// Получение IP-адреса
		public IPAddress GetIP(TcpClient client = null)
		{
			if (client == null) { return ((IPEndPoint)_client.Client.RemoteEndPoint).Address; }
			else { return ((IPEndPoint)client.Client.RemoteEndPoint).Address; }
		}

		// Прослушивание конкретного IP и возврат клиента
		public TcpClient GetClient(IPAddress ip, int port, bool apply = false)
		{
			TcpClient result = null;
			//TcpListener listener = new TcpListener(IPAddress.Any, port);
			//listener.Start();  // Запускаем слушатель один раз

			while (true)
			{
				//result = listener.AcceptTcpClient();  // Ожидаем подключения клиента
				result = new NetworkTools().AcceptConnection(port);
				if (GetIP(result).Equals(ip)) { break; } // Проверяем IP-адрес
				else { result.Close(); } 

				Thread.Sleep(100);
			}

			//listener.Stop();  // Останавливаем слушатель после получения правильного клиента
			if (apply) ApplyClient(result);
			return result;
		}

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
