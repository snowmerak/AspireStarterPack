
namespace Aspire.Hosting.ApplicationModel;

public class ValkeyClusterResource(string name, IResourceBuilder<ContainerResource> constructor, IResourceBuilder<ReplicaSetResource<ContainerResource>> replicaSet, int replicas): ContainerResource(name)
{
    internal static readonly string ValkeyCluterEndpoint = "cluster";

    internal static readonly string ValkeyNodeEndpoint = "node";

    internal static readonly string NodeDockerfile = "Node.Dockerfile";

    internal static readonly string ConstructorDockerfile = "Cons.Dockerfile";

    internal readonly IResourceBuilder<ContainerResource> Constructor = constructor;
    
    internal readonly IResourceBuilder<ReplicaSetResource<ContainerResource>> Nodes = replicaSet;

    internal readonly int Replicas = replicas;
}