using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LoadBalancer
{

	class Program
	{

		private static List<IPEndPoint> _servers = new List<IPEndPoint>{
			new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001),
			new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002),
			new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003)
		};

		private static readonly object _lock = new object();
		private static int _currentServerIndex = 0;

		private static readonly string _secretKey = "SuperSecretKeyForJWTTokensjdgfoasuet498u9gt8awujgre9q4yuthpaog;jawoiperytp123!";


		public static async Task Main(string[] args)
		{
			TcpListener listener = new TcpListener(IPAddress.Any, 5000);
			listener.Start();

			Console.WriteLine("Балансировачный сервер запущен на порту 5000;");

			while (true)
			{
				TcpClient client = await listener.AcceptTcpClientAsync();
				_ = Task.Run(() => HandleClientAsync(client));
			}



		}

		private static async Task HandleClientAsync(TcpClient client)
		{
			try
			{
				var cStream = client.GetStream();
				byte[] buff = new byte[1024];

				int bytesRead = await cStream.ReadAsync(buff, 0, buff.Length);

				string request = Encoding.UTF8.GetString(buff, 0, bytesRead);



				if (!ValidateJwtToken(request))
				{
					Console.WriteLine("Неверный JWT токен. Отклоняем соединение.");
					client.Dispose();
					return;
				}

				IPEndPoint selectedServer = GetNextServer();

				Console.WriteLine($"Клиент аутентифицирован. Перенаправляем на {selectedServer}");


				//TODO: Если клиент был подключен его нужно адресовать на тотже сервер.
				if (!await IsServerAvailableAsync(selectedServer))
				{
					Console.WriteLine($"Сервер {selectedServer} отдыхает. Пропускаем...");
					return;
				}

				using (TcpClient server = new TcpClient())
				{

					await server.ConnectAsync(selectedServer);

					await Task.WhenAll(
						RedirectDataAsync(client.GetStream(), server.GetStream()),
						RedirectDataAsync(server.GetStream(), client.GetStream())
					);

				}
			}
			catch (Exception ex)
			{

				Console.WriteLine($"Error connecting to server: {ex.Message} - {ex.InnerException}");

			}
			finally
			{
				client.Close();
			}
		}



		private static async Task RedirectDataAsync(NetworkStream from, NetworkStream to)
		{
			byte[] buffer = new byte[1024];
			int bytesRead;

			try
			{

				while ((bytesRead = await from.ReadAsync(buffer, 0, buffer.Length)) > 0)

				{

					await to.WriteAsync(buffer, 0, bytesRead);

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка: {ex.Message} - {ex.InnerException}");
			}
			finally
			{
				await from.DisposeAsync();
				await to.DisposeAsync();
			}
		}

		private static async Task<bool> IsServerAvailableAsync(IPEndPoint server)
		{
			try
			{
				using (TcpClient pingClient = new TcpClient())
				{
					await pingClient.ConnectAsync(server.Address, server.Port);
					Console.WriteLine($"Подключение к серверу {server} установлено.");
				}
				return true;
			}
			catch
			{

				return false;
			}
		}

		private static IPEndPoint GetNextServer()
		{

			lock (_lock)
			{
				IPEndPoint server = _servers[_currentServerIndex];
				_currentServerIndex = (_currentServerIndex + 1) % _servers.Count;
				return server;
			}
		}

		private static bool ValidateJwtToken(string token)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var key = Encoding.UTF8.GetBytes(_secretKey);

				tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false
				}, out SecurityToken validatedToken);

				return validatedToken != null;

			}
			catch
			{
				return false;
			}

		}
	}
}