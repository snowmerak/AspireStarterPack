
using Microsoft.AspNetCore.Routing.Matching;
using System.Text;

namespace Aspire.Hosting;

public static class ValkeyClusterExtensions
{
    public static IResourceBuilder<ValkeyClusterResource> AddValkeyCluster(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string valkeyDockerfileFolder, int nodes = 3, int replicas = 0, string? password = null, int maxClients = 10000, int maxMemoryMb = 256)
    {
        var constBuilder = builder.AddDockerfile($"{name}-constructor", valkeyDockerfileFolder,
            dockerfilePath: ValkeyClusterResource.ConstructorDockerfile)
            .WithEnvironment("REPLICAS", replicas.ToString());
        if (!string.IsNullOrEmpty(password))
        {
            constBuilder.WithEnvironment("PASSWORD", password);
        }
        var nodeAddresses = new List<Func<string>>();
        var replicaSetBuilder = builder.AddReplicaSet($"{name}-replicas", 
            (builder, s) =>
            {
                var nodeBuilder = builder.AddDockerfile(s, valkeyDockerfileFolder,
                        dockerfilePath: ValkeyClusterResource.NodeDockerfile)
                    .WithVolume($"{s}-volume", "/data")
                    .WithEndpoint(name: ValkeyClusterResource.ValkeyCluterEndpoint, targetPort: 16379, env: "VALKEY_CLUSTER_PORT", isExternal: true)
                    .WithEndpoint(name: ValkeyClusterResource.ValkeyNodeEndpoint, targetPort: 6379, env: "VALKEY_PORT",
                        isExternal: true)
                    .WithEnvironment("VALKEY_BIND", "0.0.0.0")
                    .WithEnvironment("VALKEY_TCP_KEEPALIVE", "300")
                    .WithEnvironment("VALKEY_DATABASES", "1")
                    .WithEnvironment("VALKEY_MAXCLIENTS", maxClients.ToString())
                    .WithEnvironment("VALKEY_MAXMEMORY", $"{maxMemoryMb.ToString()}mb")
                    .WithEnvironment("VALKEY_CLUSTER_ENABLED", "yes")
                    .WithLifetime(ContainerLifetime.Session);
                if (!string.IsNullOrEmpty(password))
                {
                    nodeBuilder.WithBuildArg("VALKEY_PASSWORD", password);
                }
                var clusterEndpoint = nodeBuilder.GetEndpoint(ValkeyClusterResource.ValkeyCluterEndpoint);
                var nodeEndpoint = nodeBuilder.GetEndpoint(ValkeyClusterResource.ValkeyNodeEndpoint);
                nodeAddresses.Add(() => $"{nodeEndpoint.Resource.Name}:{nodeEndpoint.TargetPort?.ToString() ?? nodeEndpoint.Port.ToString()}");
                nodeBuilder.WithEnvironment("VALKEY_CLUSTER_ANNOUNCE_IP", () => nodeEndpoint.Host)
                    .WithEnvironment("VALKEY_CLUSTER_ANNOUNCE_PORT", () => nodeEndpoint.Port.ToString())
                    .WithEnvironment("VALKEY_CLUSTER_ANNOUNCE_BUS_PORT", () => clusterEndpoint.Port.ToString());
                constBuilder.WaitFor(nodeBuilder);
                return nodeBuilder;
            }, 
            [ValkeyClusterResource.ValkeyCluterEndpoint, ValkeyClusterResource.ValkeyNodeEndpoint],
            [ValkeyClusterResource.ValkeyNodeEndpoint], 
            nodes);
        constBuilder.WithEnvironment("NODES", () => string.Join(" ", nodeAddresses.Select(x => x())));
        var res = new ValkeyClusterResource(name, constBuilder, replicaSetBuilder, replicas);
        var resourceBuilder = builder.AddResource(res)
            .WithImage("k8s.gcr.io/pause", tag: "3.9");
        return resourceBuilder;
    }

    public static IResourceBuilder<ValkeyClusterResource> WithSave(this IResourceBuilder<ValkeyClusterResource> builder)
    {
        builder.Resource.Nodes.WithEnvironment("VALKEY_SAVE", "true");

        return builder;
    }

    public static IResourceBuilder<ValkeyClusterResource> WithAppendOnly(
        this IResourceBuilder<ValkeyClusterResource> builder, AppendSync appendSync = AppendSync.EverySec)
    {
        builder.Resource.Nodes.WithEnvironment("VALKEY_AOF_ENABLED", "yes")
            .WithEnvironment("VALKEY_APPENDSYNC", () => appendSync switch
            {
                AppendSync.Always => "always",
                AppendSync.EverySec => "everysec",
                AppendSync.No => "no",
                _ => "everysec"
            });

        return builder;
    }
    
    public static IResourceBuilder<ValkeyClusterResource> InjectReplicaReferenceTo(
        this IResourceBuilder<ValkeyClusterResource> builder, IResourceBuilder<IResourceWithEnvironment> resourceBuilder)
    {
        builder.Resource.Nodes.InjectReferenceTo(resourceBuilder);
        
        resourceBuilder.WithEnvironment($"{builder.Resource.Name}__nodes", () =>
        {
            var endpoints = new List<string>();
            foreach (var replica in builder.Resource.Nodes.Resource.Replicas)
            {
                var endpoint = replica.GetEndpoint(ValkeyClusterResource.ValkeyNodeEndpoint);
                endpoints.Add($"{endpoint.Host}:{endpoint.Port.ToString()}");
            }
            return string.Join(",", endpoints);
        });

        return builder;
    }
    
    public static IResourceBuilder<ValkeyClusterResource> MakeWaitMe<T>(this IResourceBuilder<ValkeyClusterResource> builder, IResourceBuilder<T> resourceBuilder) where T: IResourceWithWaitSupport, IResourceWithEnvironment
    {
        resourceBuilder.WaitFor(builder.Resource.Constructor);
        foreach (var replica in builder.Resource.Nodes.Resource.Replicas)
        {
            resourceBuilder.WaitFor(replica);
        }

        return builder;
    }
}

public enum AppendSync
{
    Always,
    EverySec,
    No
}