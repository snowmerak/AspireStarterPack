using System.Collections.Immutable;

namespace Aspire.Hosting;

public static class ReplicaSetExtensions
{
    public static IResourceBuilder<ReplicaSetResource<T>> AddReplicaSet<T>(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, Func<IDistributedApplicationBuilder, string, IResourceBuilder<T>> constructor, ImmutableArray<string>? interReplicaEndpoints = null, ImmutableArray<string>? outboundReplicaEndpoints = null, int replicas = 1) where T : IResourceWithEndpoints, IResourceWithEnvironment
    {
        var replicaSet = new ReplicaSetResource<T>(name)
        {
            InterReplicaEndpoints = interReplicaEndpoints,
            OutboundReplicaEndpoints = outboundReplicaEndpoints,
        };
        
        foreach (var replicaName in ReplicaSetUtils.MakeReplicaNames(name, replicas))
        {
            var replica = constructor(builder, replicaName);
            replicaSet.Replicas.Add(replica);
        }
        
        foreach (var endpoint in interReplicaEndpoints ?? [])
        {
            for (var f = 0; f < replicaSet.Replicas.Count; f++)
            {
                for (var s = 0; s < replicaSet.Replicas.Count; s++)
                {
                    if (f == s) continue;
                    replicaSet.Replicas[f].WithReference(replicaSet.Replicas[s].GetEndpoint(endpoint));
                }
            }
        }
        
        return builder.AddResource(replicaSet);
    }
    
    public static IResourceBuilder<ReplicaSetResource<T>> InjectReferenceTo<T>(
        this IResourceBuilder<ReplicaSetResource<T>> builder, IResourceBuilder<IResourceWithEnvironment> resourceBuilder) where T : IResourceWithEndpoints
    {
        var replicas = new List<string>();
        
        foreach (var endpoint in builder.Resource.OutboundReplicaEndpoints ?? [])
        {
            var endpoints = new List<string>();
            
            foreach (var replica in builder.Resource.Replicas)
            {
                resourceBuilder.WithReference(replica.GetEndpoint(endpoint));
                endpoints.Add(ReplicaSetUtils.MakeServiceEndpointName(replica.Resource.Name, endpoint));
            }

            var replicaSetName = ReplicaSetUtils.MakeReplicaSetName(builder.Resource.Name, endpoint);
            resourceBuilder.WithEnvironment(replicaSetName, () => string.Join(",", endpoints));
            
            replicas.Add(replicaSetName);
        }

        resourceBuilder.WithEnvironment(ReplicaSetUtils.MakeReplicaSetEnvKey, () => string.Join(",", replicas));
        
        return builder;
    }
}

internal static class ReplicaSetUtils
{
    internal static string MakeReplicaSetEnvKey => "ReplicaSet__Names";
    
    internal static string MakeServiceEndpointName(string name, string endpoint) => $"services__{name}__{endpoint}__0";
    
    internal static string[] MakeReplicaNames(string name, int count)
    {
        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            names[i] = $"{name}-{i}";
        }
        return names;
    }

    internal static string MakeReplicaSetName(string name, string endpoint) => $"ReplicaSet__{name}__{endpoint}";
}