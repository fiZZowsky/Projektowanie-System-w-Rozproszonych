using Server.Models;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Server.Services;

public class UserService
{
    private readonly string _storagePath;
    private readonly int _port;
    private static readonly TimeSpan FileLockTimeout = TimeSpan.FromSeconds(AppConfig.FileLockTimeout);
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(AppConfig.InactivityCheckTime);
    private static readonly object ActiveUsersLock = new object();
    private static Dictionary<string, DateTime> ActiveUsers = new Dictionary<string, DateTime>();
    private static readonly SemaphoreSlim ActiveUsersSemaphore = new SemaphoreSlim(1, 1);

    public UserService(string storagePath, int port)
    {
        _storagePath = storagePath;
        _port = port;
    }

    public async Task<List<UserModel>> GetUsers()
    {
        var usersJson = await File.ReadAllTextAsync(_storagePath);
        var users = JsonConvert.DeserializeObject<List<UserModel>>(usersJson) ?? new List<UserModel>();

        return users;
    }

    public async Task<bool> AddNewUser(List<UserModel> users)
    {
        var fileInfo = new FileInfo(_storagePath);
        var startTime = DateTime.UtcNow;

        try
        {
            if (fileInfo.Exists)
            {
                bool isFileLocked = true;

                while (isFileLocked)
                {
                    try
                    {
                        // Spróbuj otworzyć plik z możliwością współdzielenia
                        using (FileStream fs = new FileStream(_storagePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            isFileLocked = false;
                        }
                    }
                    catch (IOException)
                    {
                        if (DateTime.UtcNow - startTime > FileLockTimeout)
                        {
                            return false;
                        }

                        await Task.Delay(2000); // Poczekaj 2000 ms (2 sekundy) przed kolejną próbą
                    }
                }
            }

            // Jeśli plik jest ustawiony na tylko do odczytu, zmień go na odczyt/zapis
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            var serializedList = JsonConvert.SerializeObject(users, Formatting.Indented);
            await File.WriteAllTextAsync(_storagePath, serializedList);
            return true;
        }
        catch (Exception ex)
        {
            // Loguj błąd lub dodaj szczegóły w przypadku wyjątku
            return false;
        }
        finally
        {
            // Po zakończeniu operacji, upewnij się, że plik ma odpowiedni atrybut
            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
            }
        }
    }

    public async Task<bool> PingToUser(string userId, bool IsLoggedOut)
    {
        try
        {
            await ActiveUsersSemaphore.WaitAsync(); // Asynchroniczne oczekiwanie na dostęp do sekcji krytycznej
            if(IsLoggedOut == true)
            {
                ActiveUsers.Remove(userId);
                AnnounceActiveUsersListChange();
            }
            else
            {
                ActiveUsers[userId] = DateTime.UtcNow;
            }
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ActiveUsersSemaphore.Release(); // Zwalnianie blokady
        }
    }

    public void RemoveInactiveUsers()
    {
        lock (ActiveUsersLock)
        {
            var now = DateTime.UtcNow;
            var inactiveUsers = ActiveUsers
                .Where(kvp => now - kvp.Value > InactivityTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var userId in inactiveUsers)
            {
                ActiveUsers.Remove(userId);
                Console.WriteLine($"[Server] User {userId} removed due to inactivity.");
            }

            if(inactiveUsers.Count > 0)
            {
                AnnounceActiveUsersListChange();
            }
        }
    }

    public void UpdateActiveUsersList(List<string>? usersId)
    {
        if (usersId == null || !usersId.Any())
        {
            return;
        }

        lock (ActiveUsersLock)
        {
            foreach (var userId in usersId)
            {
                if (!ActiveUsers.ContainsKey(userId)) // Jeśli użytkownika nie ma w ActiveUsers
                {
                    ActiveUsers[userId] = DateTime.UtcNow; // Dodaj użytkownika z aktualnym czasem
                    Console.WriteLine($"[Server] Added active user {userId}.");
                    AnnounceActiveUsersListChange();
                }
            }
        }
    }

    private void AnnounceActiveUsersListChange()
    {
        var activeUsersKeys = ActiveUsers.Select(n => n.Key);
        var serializedClientListJson = System.Text.Json.JsonSerializer.Serialize(activeUsersKeys);
        using var client = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

        byte[] message = Encoding.UTF8.GetBytes($"[{_port}]Clients:{serializedClientListJson}");
        client.Send(message, message.Length, endPoint);

        Console.WriteLine($"[UserService] Announced updated clients list.");
    }
}