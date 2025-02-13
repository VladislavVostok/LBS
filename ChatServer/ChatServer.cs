using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{


	public class ChatServer
	{
		private static readonly List<TcpClient> _clients = new List<TcpClient>();
		private static readonly int _port = 6000; // Порт для чат-сервера

		static async Task Main(string[] args)
		{
			TcpListener listener = new TcpListener(IPAddress.Any, _port);
			listener.Start();
			Console.WriteLine($"Chat Server запущен на порту {_port}");
			while (true)
			{
				TcpClient client = await listener.AcceptTcpClientAsync();
				_clients.Add(client);
				_ = Task.Run(() => HandleClientAsync(client));
			}
		}


		private static async Task HandleClientAsync(TcpClient client)
		{
			using (client)
			{
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[1024];
				try
				{
					while (true)
					{


						int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
						if (bytesRead == 0) break; // Клиент отключился

						string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
						Console.WriteLine($"Получено сообщение: {message}");
						
						await BroadcastMessageAsync(message, client);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка: {ex.Message}");
				}
				finally
				{
					_clients.Remove(client);
				}

			}
		}

		private static async Task BroadcastMessageAsync(string message, TcpClient clientTcp)
		{
			byte[] data = Encoding.UTF8.GetBytes(message);
			foreach (var client in _clients.Where(x => x != clientTcp))
			{
				try
				{
					
					await client.GetStream().WriteAsync(data, 0, data.Length);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
				}
			}

		}
	}
}
