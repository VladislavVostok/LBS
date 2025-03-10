﻿using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using AuthServer.DTOModels;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;

namespace AuthServer
{

	internal class AuthServer
	{

		private static readonly Dictionary<string, string> _users = new()
		{
			{ "pl1", "qwerty" },
			{ "pl2", "qwerty" }
		};

		private static readonly string _secretKey = "SuperSecretKijdfgjaeoiyj34o9uyjhuwierfjhoiqejy0ju5490hjueoifrhoijaasdeyForJWTToken123!";

		static async Task Main(string[] args)
		{
			var certificate = new X509Certificate2("serverert.pfx", "qwerty123");

			TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 4000);
			listener.Start();
			Console.WriteLine("Auth Server запущен на порту 4000...");

			while (true)
			{
				TcpClient client = await listener.AcceptTcpClientAsync();
				_ = Task.Run(() => HandleClientAsync(client, certificate));
			}

		}

		private static async Task HandleClientAsync(TcpClient client, X509Certificate2 certificate)
		{
			using (client)
			{
  				SslStream sslStream = new SslStream(client.GetStream(), false);

				// Аутентифицируемся как TLS-сервер
				await sslStream.AuthenticateAsServerAsync(
					certificate,
					clientCertificateRequired: false,
					enabledSslProtocols: SslProtocols.Tls12,
					checkCertificateRevocation: false
				);



				byte[] buffer = new byte[1024];
				int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
				string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				if (bytesRead == 0)
				{
					// Клиент отключился раньше
					return;
				}

				var credentials = JsonSerializer.Deserialize<LoginRequest>(request);
				string response;

				if (_users.TryGetValue(credentials.Username, out var password) && password == credentials.Password)
				{
					string token = GenerateJwtToken(credentials.Username);
					response = JsonSerializer.Serialize(new { Success = true, Token = token });
				}
				else
				{
					response = JsonSerializer.Serialize(new { Success = false, Token = "Invalid credentials" });
				}

				byte[] responseData = Encoding.UTF8.GetBytes(response);
				await sslStream.WriteAsync(responseData, 0, responseData.Length);
			}
		}

		private static string GenerateJwtToken(string username)
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
				expires: DateTime.UtcNow.AddHours(1),
				signingCredentials: credentials);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}

