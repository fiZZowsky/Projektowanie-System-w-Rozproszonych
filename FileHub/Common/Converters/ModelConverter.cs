using Common.Models;

namespace Common.Converters;

public static class ModelConverter
{
    public static List<NodeInfo> FromNodeListResponse(Common.GRPC.NodeListResponse nodeListResponse)
    {
        return nodeListResponse.Nodes
            .Select(node => new NodeInfo
            {
                Address = node.Address,
                Port = node.Port
            })
            .ToList();
    }

    public static Common.GRPC.NodeListResponse FromDHTNodes(List<DHTNode> nodeList)
    {
        var nodeListResponse = new Common.GRPC.NodeListResponse();
        nodeListResponse.Nodes.AddRange(
            nodeList.Select(node => new Common.GRPC.NodeInfo
            {
                Address = node.Address,
                Port = node.Port
            })
        );

        return nodeListResponse;
    }
}
