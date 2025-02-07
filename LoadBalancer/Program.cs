using System.Net;
using System.Net.Sockets;

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
            IPEndPoint selectedServer = GetNextServer();

            Console.WriteLine($"Клиент перенаправлен на севре {selectedServer}");



            try
            {
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
                from.Close();
                to.Close();
            }
        }

        private static async Task<bool> IsServerAvailableAsync(IPEndPoint server)
        {
            try
            {
                using var pingClient = new TcpClient();
                await pingClient.ConnectAsync(server.Address, server.Port);
                Console.WriteLine($"Подключение к серверу {server} установлено.");
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


    }
}