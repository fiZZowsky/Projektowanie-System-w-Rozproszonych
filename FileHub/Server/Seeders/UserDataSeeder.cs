using Common.Converters;
using Newtonsoft.Json;
using Server.Models;

namespace Server.Seeders
{
    public static class UserDataSeeder
    {
        public static string SeedDefaultUserData()
        {
            var usersList = new List<UserModel>();
            var user = new UserModel
            {
                Id = 1,
                Username = "admin",
                Password = PasswordEncryptor.EncryptPassword("root123")
            };
            usersList.Add(user);
            var serializedUser = JsonConvert.SerializeObject(usersList, Formatting.Indented);
            return serializedUser;
        }
    }
}