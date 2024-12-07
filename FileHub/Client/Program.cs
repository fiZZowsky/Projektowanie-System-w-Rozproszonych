using Client.Services;
using System.IO;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Podaj adres serwera (np. http://localhost:5000): ");
            var serverAddress = Console.ReadLine();

            var clientService = new ClientService(serverAddress);

            Console.WriteLine("1. Wyślij plik");
            Console.WriteLine("2. Pobierz plik");

            var choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.Write("Podaj nazwę pliku do wysłania: ");
                var fileName = Console.ReadLine();

                if (File.Exists(fileName))
                {
                    var fileContent = File.ReadAllBytes(fileName);
                    await clientService.UploadFileAsync(fileName, fileContent);
                }
                else
                {
                    Console.WriteLine($"Plik {fileName} nie istnieje.");
                }
            }
            else if (choice == "2")
            {
                Console.Write("Podaj nazwę pliku do pobrania: ");
                var fileName = Console.ReadLine();
                await clientService.DownloadFileAsync(fileName);
            }
        }
    }
}
