using Newtonsoft.Json;
using Server.Models;

namespace Server.Services;

public class UserService
{
    private readonly string _storagePath;
    private static readonly TimeSpan FileLockTimeout = TimeSpan.FromSeconds(300); // Timeout na 5 minut dla zablokowanego pliku

    public UserService(string storagePath)
    {
        _storagePath = storagePath;
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

                        await Task.Delay(2000); // Poczekaj 2000 ms przed kolejną próbą
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
}