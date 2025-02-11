using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer
{

	class LoginRequest
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	class AuthResponse
	{
		public bool Success { get; set; }
		public string Token { get; set; }
	}

	public class Program
	{

		private static readonly Dictionary<string, string> _users = new()
		{
			{ "player1", "password123" },
			{ "player2", "qwerty" }
		};

		private static readonly string _secretKey = "SuperSecretKeyForJWTTokensjdgfoasuet498u9gt8awujgre9q4yuthpaog;jawoiperytp123!";

		static async Task Main(string[] args)
		{
			TcpListener listener = new TcpListener(IPAddress.Any, 4000);
			listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			listener.Start();
			Console.WriteLine("Auth Server запущен на порту 4000...");

			while (true)
			{
				TcpClient client = await listener.AcceptTcpClientAsync();
				_ = Task.Run(() => HandleClientAsync(client));
			}
		}

		private static async Task HandleClientAsync(TcpClient client)
		{

			using (client)
			{
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[1024];
				int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

				if (bytesRead > 0)
				{
					string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
					var credentials = JsonSerializer.Deserialize<LoginRequest>(request);

					string response;
					AuthResponse authResponse = new()
					{
						Token = string.Empty,
						Success = false
					};

					if (_users.TryGetValue(credentials.Username, out var password) && password == credentials.Password)
					{
						authResponse.Token = GenerateJwtToken(credentials.Username);
						authResponse.Success = true;
					}

					var responseToken = JsonSerializer.Serialize<AuthResponse>(authResponse);
					var responseByte = Encoding.UTF8.GetBytes(responseToken);
					await stream.WriteAsync(responseByte, 0, responseByte.Length);

					buffer = new byte[1024];
					bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

					Console.WriteLine($"{Encoding.UTF8.GetString(buffer, 0, bytesRead)}");
				}
			}
		}

		private static string GenerateJwtToken(string username)
		{
			try
			{
				var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
				var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

				var claims = new[]
				{
				new Claim(ClaimTypes.Name, username),
			};

				var token = new JwtSecurityToken(
					issuer: "PokerGame",
					audience: "Players",
					claims: claims,
					expires: DateTime.Now.AddHours(1),
					signingCredentials: credentials
				);
 return new JwtSecurityTokenHandler().WriteToken(token);
			}
			catch (Exception ex) {
				Console.WriteLine($"Ошибка при генерации токена: {ex.Message}");
				return null;
			}
		}

	}
}
