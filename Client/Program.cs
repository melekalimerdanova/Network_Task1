using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "File Client";

            var serverIp = IPAddress.Parse("192.168.31.185");
            int serverPort = 27001;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("İstifadəçi adınızı daxil edin: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            string userName = Console.ReadLine()?.Trim();
            Console.ResetColor();

            if (string.IsNullOrEmpty(userName))
                userName = "Tələbə";

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("========================================");
                Console.WriteLine($"         FILE CLIENT: {userName}        ");
                Console.WriteLine("========================================");
                Console.ResetColor();
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("1 - Fayl göndər (şəkil/səs)");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("0 - Çıxış");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("========================================");
                Console.ResetColor();
                Console.Write("Seçim edin: ");

                string? userChoice = Console.ReadLine();

                if (userChoice == "0")
                {
                    break;
                }

                if (userChoice == "1")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\nFaylın tam yolunu daxil edin:\n> ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    string filePath = Console.ReadLine()?.Trim('\"') ?? "";
                    Console.ResetColor();

                    if (!File.Exists(filePath))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Göstərilən ünvanda fayl tapılmadı!");
                        Console.ResetColor();
                        Thread.Sleep(1500);
                        continue;
                    }

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\nFayl serverə ötürülür...");
                    Console.ResetColor();

                    try
                    {
                        using (var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            await tcpSocket.ConnectAsync(new IPEndPoint(serverIp, serverPort));

                            byte[] userBytes = Encoding.UTF8.GetBytes(userName);
                            byte[] userLenBytes = BitConverter.GetBytes(userBytes.Length);
                            await tcpSocket.SendAsync(userLenBytes, SocketFlags.None);
                            await tcpSocket.SendAsync(userBytes, SocketFlags.None);

                            string fileNameOnly = Path.GetFileName(filePath);
                            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileNameOnly);
                            byte[] fileNameLenBytes = BitConverter.GetBytes(fileNameBytes.Length);
                            await tcpSocket.SendAsync(fileNameLenBytes, SocketFlags.None);
                            await tcpSocket.SendAsync(fileNameBytes, SocketFlags.None);

                            byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);
                            byte[] fileSizeLenBytes = BitConverter.GetBytes((long)fileContentBytes.Length);

                            await tcpSocket.SendAsync(fileSizeLenBytes, SocketFlags.None);
                            await tcpSocket.SendAsync(fileContentBytes, SocketFlags.None);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Fayl uğurla göndərildi!");
                            Console.ResetColor();

                            tcpSocket.Shutdown(SocketShutdown.Both);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Göndərmə xətası: {ex.Message}");
                        Console.ResetColor();
                    }

                    Console.WriteLine("\nƏsas menyuya qayıtmaq üçün bir düyməyə basın...");
                    Console.ReadKey();
                }
            }
        }
    }
}
