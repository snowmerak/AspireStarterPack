using System.Text;
using Microsoft.Win32;

namespace Aspire.Hosting;

public static class ContainerReplicaSetExtensions
{
    private static string MakeReplicaSetEnvKey => "ReplicaSet__Names";
    
    private static string MakeServiceEndpointName(string name, string endpoint) => $"services__{name}__{endpoint}__0";
    
    private static string[] MakeReplicaNames(string name, int count)
    {
        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            names[i] = $"{name}-{i}";
        }
        return names;
    }

    private static string MakeReplicaSetName(string name, string endpoint) => $"ReplicaSet__{name}__{endpoint}";
    
    public static IResourceBuilder<ContainerReplicaSetResource> AddContainerReplicaSet(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, Func<IDistributedApplicationBuilder, string, IResourceBuilder<ContainerResource>> constructor, string[]? interReplicaEndpoints = null, string[]? outboundReplicaEndpoints = null, int replicas = 1)
    {
        var replicaSet = new ContainerReplicaSetResource(name, interReplicaEndpoints, outboundReplicaEndpoints);
        
        foreach (var replicaName in MakeReplicaNames(name, replicas))
        {
            var replica = constructor(builder, replicaName);
            replicaSet.Replicas.Add(replica);
        }
        
        foreach (var endpoint in interReplicaEndpoints ?? Array.Empty<string>())
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
    
    public static IResourceBuilder<ContainerReplicaSetResource> InjectReferenceTo(
        this IResourceBuilder<ContainerReplicaSetResource> builder, IResourceBuilder<IResourceWithEnvironment> resourceBuilder)
    {
        var replicas = new List<string>();
        
        foreach (var endpoint in builder.Resource.OutboundReplicaEndpoints ?? Array.Empty<string>())
        {
            var endpoints = new List<string>();
            
            foreach (var replica in builder.Resource.Replicas)
            {
                resourceBuilder.WithReference(replica.GetEndpoint(endpoint));
                endpoints.Add(MakeServiceEndpointName(replica.Resource.Name, endpoint));
            }

            var replicaSetName = MakeReplicaSetName(builder.Resource.Name, endpoint);
            resourceBuilder.WithEnvironment(replicaSetName, () => string.Join(",", endpoints));
            
            replicas.Add(replicaSetName);
        }

        resourceBuilder.WithEnvironment(MakeReplicaSetEnvKey, () => string.Join(",", replicas));
        
        return builder;
    }
}