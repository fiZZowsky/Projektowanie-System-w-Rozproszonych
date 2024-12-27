namespace Common.Models;

public class NodeInfo
{
    public string Address { get; set; }
    public int Port { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is NodeInfo other)
        {
            return Address == other.Address && Port == other.Port;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Address, Port);
    }

    public override string ToString()
    {
        return $"{Address}:{Port}";
    }
}