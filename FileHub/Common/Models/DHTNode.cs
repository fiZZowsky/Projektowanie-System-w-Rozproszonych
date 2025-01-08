namespace Common.Models
{
    public class DHTNode
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public int Hash { get; set; }
        public int ResponsibleRangeStart { get; set; }
        public int ResponsibleRangeEnd { get; set; }
    }
}
