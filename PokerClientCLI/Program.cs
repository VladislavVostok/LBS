using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PokerClientCLI
{

	class AuthResponse
	{
		public bool Success { get; set; }
		public string Token { get; set; }
	}


	public class Program
	{
		private const string AuthServerIp = "127.0.0.1";
		private const int AuthServerPort = 4000;
		private const string BalancerIp = "127.0.0.1";
		private const int BalancerPort = 5000;


		static async Task Main(string[] args)
		{
			try
			{
				Console.Write("Введите логин: ");
				string username = Console.ReadLine();

				Console.Write("Введите пароль: ");
				string password = Console.ReadLine();

				string token = await GetAuthToken(username, password);

				if (string.IsNullOrEmpty(token))
				{
					Console.WriteLine("Ошибка авторизации. Завершаем работу.");
					return;
				}

				Console.WriteLine($"Токен получен: {token}\n");


				await ConnectToBalancer(token);


			}
			catch (SocketException e)
			{

				Console.WriteLine($"{e.Message} - {e.InnerException}");

			}
		}

		static byte[] getRequestBytes(string request)
		{
			return Encoding.UTF8.GetBytes(request);

		}


		private static async Task ConnectToBalancer(string token)
		{
			try {
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(BalancerIp, BalancerPort);
				NetworkStream stream = client.GetStream();

				byte[] tokenData = Encoding.UTF8.GetBytes(token);
				await stream.WriteAsync(tokenData, 0, tokenData.Length);
				byte[] buffer = new byte[1024];

				int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
				string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
				Console.WriteLine($"Ответ от сервера: {response}");

				Console.Write("Введите Команду: ");
				string command = Console.ReadLine();

				byte[] commandByte = Encoding.UTF8.GetBytes(token);
				await stream.WriteAsync(commandByte, 0, tokenData.Length);
				buffer = new byte[1024];

				bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
				response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
				Console.WriteLine($"Ответ от сервера: {response}");
			}
			catch (Exception ex)
			{

				Console.WriteLine($"Ошибка подключения к балансировщику: {ex.Message}");
			}
		}

		private static async Task<string> GetAuthToken(string username, string password)
		{

			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(AuthServerIp, AuthServerPort);
				NetworkStream stream = client.GetStream();

				var loginRequest = new { Username = username, Password = password };
				string jsonRequest = JsonSerializer.Serialize(loginRequest);
				byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);

				await stream.WriteAsync(requestData, 0, requestData.Length);
				byte[] buffer = new byte[1024];

				int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
				string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				var response = JsonSerializer.Deserialize<AuthResponse>(responseJson);
				Console.WriteLine($"Ответ от сервера: {response}");
				return response.Token.ToString();

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка подключения к балансировщику: {ex.Message}");
			}

			return string.Empty;
		}
	}
}
