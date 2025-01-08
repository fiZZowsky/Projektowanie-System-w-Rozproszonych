using Server.Seeders;

namespace Server;

public class AppConfig
{
    public const string MulticastAddress = "239.0.0.1";
    public const int MulticastPort = 5000;
    public const int StartPort = 5000;
    public const int EndPort = 6000;

    public static string DefaultFilesStoragePath
    {
        get
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var dataStoragePath = Path.Combine(Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName, "DataStorage");
            if (dataStoragePath == null || !Directory.Exists(dataStoragePath))
            {
                throw new InvalidOperationException("Invalid path for DataStorage.");
            }
            return dataStoragePath;
        }
    }

    public static string DefaultUserDataStoragePath
    {
        get
        {
            var userDataStoragePath = Path.Combine(DefaultFilesStoragePath, "UsersStorage");
            if (!Directory.Exists(userDataStoragePath))
            {
                Directory.CreateDirectory(userDataStoragePath);
            }

            userDataStoragePath = Path.Combine(userDataStoragePath, "UserData.json");
            if (!File.Exists(userDataStoragePath))
            {
                var seededUserData = UserDataSeeder.SeedDefaultUserData();
                File.WriteAllText(userDataStoragePath, seededUserData);
            }

            return userDataStoragePath;
        }
    }
}
