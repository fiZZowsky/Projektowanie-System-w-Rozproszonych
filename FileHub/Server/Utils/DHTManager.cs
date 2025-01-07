using Common.Models;
using System.Security.Cryptography;
using System.Text;

namespace Server.Utils
{
    public static class DHTManager
    {
        public static int ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToInt32(hashBytes, 0) & 0x7FFFFFFF; // Zwróć dodatni int
        }

        public static DHTNode FindResponsibleNode(string fileName, List<DHTNode> nodes)
        {
            int fileHash = ComputeHash(fileName);
            nodes = nodes.OrderBy(n => n.Hash).ToList(); // Posortuj serwery po hashach

            // Znajdź pierwszy serwer, którego hash jest większy niż hash pliku
            foreach (var node in nodes)
            {
                if (fileHash <= node.Hash)
                {
                    return node;
                }
            }

            // Jeśli żaden nie pasuje, wróć pierwszy węzeł (pierścień DHT)
            return nodes.First();
        }
    }
}
