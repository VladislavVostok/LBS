using System.Net.Sockets;
using System.Text;

namespace Client
{
	class Program
	{
		public static async Task Main()
		{
			using TcpClient client = new TcpClient();
			{
				await client.ConnectAsync("127.0.0.1", 5000);

				NetworkStream stream = client.GetStream();

				while (true)
				{
					byte[] massage = Encoding.UTF8.GetBytes("Hello, Server!");

					await stream.WriteAsync(massage, 0, massage.Length);

					byte[] buffer = new byte[1024];
					int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
					Console.WriteLine($"{client.Client.RemoteEndPoint} отправил: {Encoding.UTF8.GetString(buffer)}");
					await Task.Delay(2000);
				}
			}
		}
	}
}
