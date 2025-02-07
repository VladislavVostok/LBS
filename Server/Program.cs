using System.Net;
using System.Net.Sockets;

namespace Server
{
	class Program
	{
		public static async Task Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Нет порта!");
			}

			int port = int.Parse(args[0]);

			TcpListener listener = new TcpListener(IPAddress.Any, port);

			listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			listener.Start();

			Console.WriteLine($"Сервер запущен на порту {port}...");

			while (true)
			{
				TcpClient client = await listener.AcceptTcpClientAsync();
				_ = Task.Run(() => HandleClientAsync(client));
			}

		}

		private static async Task HandleClientAsync(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			byte[] buffer = new byte[1024];

			int bytesRead;

			try
			{
				while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await stream.WriteAsync(buffer, 0, bytesRead);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
			finally
			{
				client.Close();
			}
		}
	}
}