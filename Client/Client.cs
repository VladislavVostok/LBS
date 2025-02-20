using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

namespace Client
{
	class Client
	{
		private const string AuthServerIp = "127.0.0.1";
		private const int AuthServerPort = 4000;
		private const string BalancerIp = "127.0.0.1";
		private const int BalancerPort = 5000;
		private const string ChatServerIp = "127.0.0.1";
		private const int ChatServerPort = 6000;
		static async Task Main()
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

			// Подключаемся к балансировщику и чат-серверу
			var pokerTask = ConnectToBalancer(token);
			//var chatTask = ConnectToChatServer();

			await Task.WhenAll(pokerTask);//, chatTask);
		}


		private static async Task ConnectToBalancer(string token)
		{
			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(BalancerIp, BalancerPort);


				SslStream sslStream = new SslStream(client.GetStream(), false,
					// В демо отключаем строгую проверку сертификата сервера
					(sender, cert, chain, sslPolicyErrors) => true);

				// Аутентифицируемся как TLS-клиент
				await sslStream.AuthenticateAsClientAsync("BalancerServer", null, SslProtocols.Tls12, false);

				// Отправляем токен балансировщику
				byte[] tokenData = Encoding.UTF8.GetBytes(token);
				await sslStream.WriteAsync(tokenData, 0, tokenData.Length);

				// Читаем ответ от покерного сервера через балансировщик
				byte[] buffer = new byte[1024];
				int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
				string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				Console.WriteLine($"Ответ от балансировочного сервера: {response}");

				if (response == "OK")
				{
					while (true)
					{
						tokenData = Encoding.UTF8.GetBytes("DEAL_CARDS");
						await sslStream.WriteAsync(tokenData, 0, tokenData.Length);

						bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
						response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
						Console.WriteLine($"Ваши карты сударь: {response}");
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка подключения к балансировщику: {ex.Message}");
			}
		}

		private static async Task ConnectToChatServer()
		{
			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(ChatServerIp, ChatServerPort);
				NetworkStream stream = client.GetStream();

				// Запускаем поток для чтения сообщений из чата
				var readTask = Task.Run(async () =>
				{
					byte[] buffer = new byte[1024];
					while (true)
					{
						int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
						string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
						Console.WriteLine($"Чат: {message}");
					}
				});

				// Запускаем поток для отправки сообщений в чат
				while (true)
				{
					string message = Console.ReadLine();
					byte[] data = Encoding.UTF8.GetBytes(message);
					await stream.WriteAsync(data, 0, data.Length);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка подключения к чат-серверу: {ex.Message}");
			}
		}


		private static async Task<string> GetAuthToken(string username, string password)
		{
			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(AuthServerIp, AuthServerPort);

				SslStream sslStream = new SslStream(client.GetStream(), false,
					// В демо отключаем строгую проверку сертификата сервера
					(sender, cert, chain, sslPolicyErrors) => true);

				// Аутентифицируемся как TLS-клиент
				sslStream.AuthenticateAsClient("AuthServer", null, SslProtocols.Tls12, false);



				var loginRequest = new { Username = username, Password = password };
				string jsonRequest = JsonSerializer.Serialize(loginRequest);
				byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);

				await sslStream.WriteAsync(requestData, 0, requestData.Length);

				byte[] buffer = new byte[1024];
				int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
				string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				var response = JsonSerializer.Deserialize<AuthResponse>(responseJson);
				return response?.Success == true ? response.Token : null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка подключения к серверу авторизации: {ex.Message}");
				return null;
			}
		}


		class AuthResponse
		{
			public bool Success { get; set; }
			public string Token { get; set; }
		}
	}
}
