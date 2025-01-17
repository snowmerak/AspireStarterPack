using System.Collections.Immutable;

namespace Aspire.Hosting.ApplicationModel;

using ContainerReplicaSetResource = ReplicaSetResource<ContainerResource>;
using ExecutableReplicaSetResource = ReplicaSetResource<ExecutableResource>;

public class ReplicaSetResource<T>(string name) : ContainerResource(name) where T : IResourceWithEndpoints
{
    public ImmutableArray<string>? InterReplicaEndpoints { get; init; }
    public ImmutableArray<string>? OutboundReplicaEndpoints { get; init; }

    public readonly List<IResourceBuilder<T>> Replicas = [];
}