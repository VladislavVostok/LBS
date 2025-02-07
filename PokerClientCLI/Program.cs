using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PokerClientCLI
{
	internal class Program
	{

		public class ServerEndPoint { 
			public string Host { get; set; }
			public int Port { get; set; }

			public override string ToString()
			{
				return Host + ":" + Port;
			}

		}

		static List<ServerEndPoint> EndPoints = new();



		static async Task Main(string[] args)
		{

			string serverIp = "127.0.0.1";
			int serverPort = 5000; // Подключаемся к балансировщику

			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(serverIp, serverPort);
				NetworkStream stream = client.GetStream();

				while (true) {

					Console.WriteLine("Выберите пункт меню:\n" +
						"1 - Получить список доступных серверов.\n" +
						"2 - Подключиться к свыбранному серверу");
					
					int menu_item = Convert.ToInt32(Console.ReadLine());

					switch (menu_item){
						case 1:
							string request = "GET_SERVERS";
							byte[] requestData = getRequestBytes(request);
							await stream.WriteAsync(requestData, 0, requestData.Length);
							
							byte[] buff = new byte[1024];
							int byteRead = await stream.ReadAsync(buff, 0, buff.Length);
							
							string response = Encoding.UTF8.GetString(buff, 0, byteRead);

							string[] servers = response.Split(" | ");

							foreach(string s in servers)
							{
								EndPoints.Add(new ServerEndPoint
								{
									Host = s.Trim().Split(":")[0],
									Port = Convert.ToInt32(s.Trim().Split(":")[1])

								});
							}


							for (int i = 0; i < EndPoints.Count; i++)
							{
								Console.WriteLine($"{i + 1} -> {EndPoints[i].ToString()}");
							}




							break;
						case 2:

							Console.WriteLine("Введите номер сервера: ");
							int conSer = Convert.ToInt32(Console.ReadLine());

							request = "CONNET_TO " + conSer.ToString();
							requestData = getRequestBytes(request);
							await stream.WriteAsync(requestData, 0, requestData.Length);
							
							buff = new byte[1024];
							byteRead = await stream.ReadAsync(buff, 0, buff.Length);
							
		
							response = Encoding.UTF8.GetString(buff, 0, byteRead);


							if (response == "OK")
							{

								Console.WriteLine("Сервер готов!");


							}
							else
							{
								Console.WriteLine("Выберите другой!");
							}

							break;
						case 3: break;
						case 4: break;
					}





				}


			}
			catch { 
			
			
			}
		}

		static byte[] getRequestBytes(string request)
		{
			return Encoding.UTF8.GetBytes(request);

		}

	}
}
