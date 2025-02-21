using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Server
{
    class Server
    {
        private static readonly string[] Cards = {
            "2H", "3H", "4H", "5H", "6H", "7H", "8H", "9H", "10H",
            "JH", "QH", "KH", "AH",
            "2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D",
            "JD", "QD", "KD", "AD",
            "2C", "3C", "4C", "5C", "6C", "7C", "8C", "9C", "10C",
            "JC", "QC", "KC", "AC",
            "2S", "3S", "4S", "5S", "6S", "7S", "8S", "9S", "10S",
            "JS", "QS", "KS", "AS"
        };

        private static readonly Random Random = new Random();

        static async Task Main(string[] args)
        {
            // Загружаем сертификат
            var certificate = new X509Certificate2("serverert.pfx", "qwerty123");

            // По умолчанию порт 5001, если не передан аргумент
            int port = (args.Length == 0) ? 5001 : int.Parse(args[0]);

            // Создаём TCP-сокет
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();

            Console.WriteLine($"[PokerServer] Сервер запущен на порту {port}...");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client, certificate));
            }
        }

        private static async Task HandleClientAsync(TcpClient client, X509Certificate2 certificate)
        {
            try
            {
                using (client)
                using (var sslStream = new SslStream(
                    client.GetStream(),
                    false,
                    (sender, cert, chain, sslPolicyErrors) => true))
                {
                    // ---- Сервер должен быть "TLS-сервером" ----
                    await sslStream.AuthenticateAsServerAsync(
                        certificate,
                        clientCertificateRequired: false,
                        enabledSslProtocols: SslProtocols.Tls12,
                        checkCertificateRevocation: false
                    );

                    Console.WriteLine($"[PokerServer] Подключился клиент: {client.Client.RemoteEndPoint}");

                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead <= 0) break; // клиент отключился

                        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        Console.WriteLine($"[PokerServer] Получен запрос \"{request}\" от {client.Client.RemoteEndPoint}");

                        // Если запрос на проверку "жив ли сервер" (ALIVIE)
                        if (request == "ALIVIE")
                        {
                            byte[] responseData = Encoding.UTF8.GetBytes("I_AM_ALIVE");
                            await sslStream.WriteAsync(responseData, 0, responseData.Length);
                        }

                        // Если запрос на выдачу карт
                        if (request == "DEAL_CARDS")
                        {
                            string response = DealCards();
                            byte[] responseData = Encoding.UTF8.GetBytes(response);
                            await sslStream.WriteAsync(responseData, 0, responseData.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PokerServer] Ошибка: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        private static string DealCards()
        {
            var dealtCards = new HashSet<string>();
            while (dealtCards.Count < 2)
            {
                dealtCards.Add(Cards[Random.Next(Cards.Length)]);
            }
            return string.Join(", ", dealtCards);
        }
    }
}
