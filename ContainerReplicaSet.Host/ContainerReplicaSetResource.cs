namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;

public class ContainerReplicaSetResource(
    string name,
    string[]? interReplicaEndpoints = null,
    string[]? outboundReplicaEndpoints = null) : ContainerResource(name)
{
    internal readonly string[]? InterReplicaEndpoints = interReplicaEndpoints;
    internal readonly string[]? OutboundReplicaEndpoints = outboundReplicaEndpoints;

    internal readonly List<IResourceBuilder<ContainerResource>> Replicas = [];
}