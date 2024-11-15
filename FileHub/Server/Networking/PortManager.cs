namespace Server.Networking
{
    public class PortManager
    {
        private readonly int _minPort;
        private readonly int _maxPort;
        private readonly HashSet<int> _usedPorts;

        public PortManager(int minPort, int maxPort)
        {
            _minPort = minPort;
            _maxPort = maxPort;
            _usedPorts = new HashSet<int>();
        }

        public int? AssignPort()
        {
            for (int port = _minPort; port <= _maxPort; port++)
            {
                if (!_usedPorts.Contains(port))
                {
                    _usedPorts.Add(port);
                    return port;
                }
            }

            return null;
        }

        public void ReleasePort(int port)
        {
            _usedPorts.Remove(port);
        }
    }
}
