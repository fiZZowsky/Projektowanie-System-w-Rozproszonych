using Server.Models;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Common.Models;

namespace Server.Services;

public class UserService
{
    private readonly string _storagePath;
    private readonly int _port;
    private static readonly TimeSpan FileLockTimeout = TimeSpan.FromSeconds(AppConfig.FileLockTimeout);
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(AppConfig.InactivityCheckTime);
    private static readonly object ActiveUsersLock = new object();
    private static List<ActiveUserModel> ActiveUsers = new List<ActiveUserModel>();
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

    public async Task<bool> PingToUser(string computerId, bool IsLoggedOut)
    {
        try
        {
            await ActiveUsersSemaphore.WaitAsync(); // Asynchroniczne oczekiwanie na dostęp do sekcji krytycznej
            var user = ActiveUsers.FirstOrDefault(user => user.ComputerId == computerId);
            if (IsLoggedOut == true)
            {
                ActiveUsers.Remove(user);
                AnnounceActiveUsersListChange();
            }
            else
            {
                user.LastPing = DateTime.UtcNow;
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
                .Where(kvp => now - kvp.LastPing > InactivityTimeout)
                .Select(kvp => kvp.ComputerId)
                .ToList();

            foreach (var computerId in inactiveUsers)
            {
                var user = ActiveUsers.FirstOrDefault(x => x.ComputerId == computerId);
                ActiveUsers.Remove(user);
                Console.WriteLine($"[Server] User {computerId} removed due to inactivity.");
            }

            if(inactiveUsers.Count > 0)
            {
                AnnounceActiveUsersListChange();
            }
        }
    }

    public void UpdateActiveUsersList(List<ActiveUserModel> users)
    {
        if (users == null || !users.Any())
        {
            return;
        }

        lock (ActiveUsersLock)
        {
            foreach (var user in users)
            {
                var res = ActiveUsers.FirstOrDefault(x => x.ComputerId == user.ComputerId);
                if (res == null) // Jeśli użytkownika nie ma w ActiveUsers
                {
                    user.LastPing = DateTime.UtcNow;
                    ActiveUsers.Add(user); // Dodaj użytkownika z aktualnym czasem
                    Console.WriteLine($"[Server] Added active user {user.UserId}.");
                    AnnounceActiveUsersListChange();
                }
            }
        }
    }

    private void AnnounceActiveUsersListChange()
    {
        var activeUsersInfo = ActiveUsers.Select(user => new { user.Id, user.ComputerId });
        var serializedClientListJson = System.Text.Json.JsonSerializer.Serialize(activeUsersInfo);
        using var client = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

        byte[] message = Encoding.UTF8.GetBytes($"[{_port}]Clients:{serializedClientListJson}");

        client.Send(message, message.Length, endPoint);

        Console.WriteLine($"[UserService] Announced updated clients list.");
    }

    public List<ActiveUserModel> GetUsersToSync(string userId, string computerId)
    {
        return ActiveUsers
        .Where(x => x.UserId == userId && x.ComputerId != computerId)
        .ToList();
    }
}