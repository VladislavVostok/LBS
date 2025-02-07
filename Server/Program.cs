using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
	class Program
	{
		public static async Task Main(string[] args)
		{
			int port = args.Length == 0 ? 5001 : int.Parse(args[0]);


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
					Console.WriteLine($"{client.Client.RemoteEndPoint} отправил: {Encoding.UTF8.GetString(buffer)}");
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