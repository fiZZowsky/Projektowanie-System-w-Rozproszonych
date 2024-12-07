using Common.GRPC;
using Common.Models;
using Common.Utils;
using Grpc.Net.Client;

namespace Server.Services
{
    internal class DHTService
    {
        private readonly int _port;
        private readonly List<DHTNode> _nodes = new List<DHTNode>();

        public DHTService(int port)
        {
            _port = port;
        }
        public void AddNode(int port)
        {
            var nodeHash = DHTManager.ComputeHash(port.ToString());
            if (!_nodes.Any(n => n.Port == port))
            {
                _nodes.Add(new DHTNode { Address = "localhost", Port = port, Hash = nodeHash });
                _nodes.Sort((a, b) => a.Hash.CompareTo(b.Hash));
                Console.WriteLine($"[DHT] Added node: {port} (Hash: {nodeHash}).");
                RecalculateResponsibilities();
            }
        }

        public void RemoveNode(int port)
        {
            var node = _nodes.FirstOrDefault(n => n.Port == port);
            if (node != null)
            {
                _nodes.Remove(node);
                Console.WriteLine($"[DHT] Removed node: {port}.");
                RecalculateResponsibilities();
            }
        }

        private void RecalculateResponsibilities()
        {
            Console.WriteLine("[DHT] Recalculating responsibilities...");
            foreach (var node in _nodes)
            {
                // Przykład: wypisz zakres odpowiedzialności
                Console.WriteLine($"Node {node.Port}: Responsible for hash range...");
                // Możesz dodać bardziej szczegółową logikę dla zakresów odpowiedzialności
            }
        }

        public List<DHTNode> GetNodes() => _nodes;
    }
}
