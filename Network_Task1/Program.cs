using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Network_Task1 
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "File Server";

            var ipAddress = IPAddress.Parse("192.168.31.185");
            int portNumber = 27001;

            using (var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.Bind(new IPEndPoint(ipAddress, portNumber));
                serverSocket.Listen(10);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("========================================");
                Console.WriteLine("               FILE SERVER              ");
                Console.WriteLine("========================================");
                Console.ResetColor();
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Server işə düşdü və dinlənilir...");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Port: {portNumber}");
                Console.ResetColor();

                Console.WriteLine("Client-lərdən fayllar gözlənilir...\n");

                while (true)
                {
                    var clientSocket = await serverSocket.AcceptAsync();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string clientAddress = clientSocket.RemoteEndPoint?.ToString() ?? "Bilinmir";

                            byte[] nameLenBuffer = new byte[4];
                            await ReadDataAsync(clientSocket, nameLenBuffer, 4);
                            int nameLen = BitConverter.ToInt32(nameLenBuffer, 0);

                            byte[] nameBuffer = new byte[nameLen];
                            await ReadDataAsync(clientSocket, nameBuffer, nameLen);
                            string username = Encoding.UTF8.GetString(nameBuffer);

                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.Write("[QOŞULDU] ");
                            Console.ResetColor();
                            Console.WriteLine($"{username} ({clientAddress}) sistemə bağlandı.");

                            byte[] fileLenBuffer = new byte[4];
                            await ReadDataAsync(clientSocket, fileLenBuffer, 4);
                            int fileNameLength = BitConverter.ToInt32(fileLenBuffer, 0);

                            byte[] fileNameBuffer = new byte[fileNameLength];
                            await ReadDataAsync(clientSocket, fileNameBuffer, fileNameLength);
                            string targetFileName = Encoding.UTF8.GetString(fileNameBuffer);

                            byte[] sizeBuffer = new byte[8];
                            await ReadDataAsync(clientSocket, sizeBuffer, 8);
                            long totalFileLength = BitConverter.ToInt64(sizeBuffer, 0);

                            string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, targetFileName);
                            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                            {
                                byte[] dataBuffer = new byte[8192];
                                long receivedBytesCount = 0;

                                while (receivedBytesCount < totalFileLength)
                                {
                                    int readChunk = (int)Math.Min(dataBuffer.Length, totalFileLength - receivedBytesCount);
                                    int bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(dataBuffer, 0, readChunk), SocketFlags.None);

                                    if (bytesRead == 0) break;

                                    await fileStream.WriteAsync(dataBuffer, 0, bytesRead);
                                    receivedBytesCount += bytesRead;
                                }
                            }

                            string formattedFileSize = totalFileLength > 1024 * 1024
                                ? $"{totalFileLength / (1024.0 * 1024.0):F1} MB"
                                : $"{totalFileLength / 1024.0:F1} KB";

                            string logTime = DateTime.Now.ToString("HH:mm:ss");

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("[QƏBUL EDİLDİ] ");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write($"{targetFileName,-12} ({formattedFileSize,7}) ");
                            Console.ResetColor();
                            Console.WriteLine($"| Göndərən: {username} [{clientAddress}]   {logTime}");
                        }
                        catch (Exception err)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Xəta baş verdi: {err.Message}");
                            Console.ResetColor();
                        }
                        finally
                        {
                            clientSocket.Dispose();
                        }
                    });
                }
            }
        }
        private static async Task ReadDataAsync(Socket sock, byte[] buffer, int size)
        {
            int totalRead = 0;
            while (totalRead < size)
            {
                int read = await sock.ReceiveAsync(new ArraySegment<byte>(buffer, totalRead, size - totalRead), SocketFlags.None);
                if (read == 0) throw new SocketException();
                totalRead += read;
            }
        }
    }
}