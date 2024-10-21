using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SquadVoiceServer
{
	internal class NetworkTools
	{
		private NetworkStream _stream;
		private byte[] bufferOut;

		//написать методц принятия и отправки пакетов
		public NetworkTools(NetworkStream stream)
		{
			_stream = stream;
		}

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

		// Метод для закрытия потока
		public void CloseStream()
		{
			_stream?.Close();
		}
	}
}
