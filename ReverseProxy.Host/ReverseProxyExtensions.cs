using System.Reflection;

namespace Aspire.Hosting;

public static class ReverseProxyExtensions
{
    public static IResourceBuilder<ReverseProxyResource> AddReverseProxy(this IDistributedApplicationBuilder builder, [ResourceName] string name, ProjectResourceOptions? opt = null)
    {
        ReverseProxyResource res = new(name);
        return builder.AddResource(res)
            .WithAnnotation(new Projects.ReverseProxy())
            .WithEnvironment("routes", () => string.Join(",", res.routeMap.Keys))
            .WithReverseProxyDefaults(opt ?? new());
    }
    static IResourceBuilder<ReverseProxyResource> WithReverseProxyDefaults(this IResourceBuilder<ReverseProxyResource> builder, ProjectResourceOptions options) => whatthehack(builder, options) as IResourceBuilder<ReverseProxyResource> ?? builder; //todo: need self implement
    static readonly FuncWithReverseProxyDefaults whatthehack = typeof(ProjectResourceBuilderExtensions).GetMethod("WithProjectDefaults", BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate<FuncWithReverseProxyDefaults>();
    delegate IResourceBuilder<ProjectResource> FuncWithReverseProxyDefaults(IResourceBuilder<ProjectResource> builder, ProjectResourceOptions options);

    public static IResourceBuilder<ReverseProxyResource> WithReplicas(this IResourceBuilder<ReverseProxyResource> builder, int replicas) => builder.WithAnnotation(new ReplicaAnnotation(replicas));

    public static IResourceBuilder<ReverseProxyResource> AddService(this IResourceBuilder<ReverseProxyResource> builder, string route, IResourceBuilder<IResourceWithServiceDiscovery> resourceBuilder, int replica = 1)
    {
        var map = builder.Resource.routeMap;
        if (map.TryGetValue(route, out var list) is not true)
        {
            map[route] = list = [];
            builder.WithEnvironment($"route:{route}", () => string.Join(",", list));
        }
        for (var i = 0; i < replica; i++)
            list.Add($"services__{resourceBuilder.Resource.Name}__http__{i}");
        return builder.WithReference(resourceBuilder);
    }
}