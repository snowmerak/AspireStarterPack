using System.Collections.Immutable;
using Aspire.Hosting;
using Projects;

namespace AppHost.ReverseProxy;

using Aspire.Hosting.ApplicationModel;

public class Configurator
{
    private readonly Dictionary<string, List<string>> _routeMap = new ();
    private readonly IResourceBuilder<ProjectResource> _resourceBuilder;
    
    public Configurator(IDistributedApplicationBuilder builder, int replica = 1, int port = 5000)
    {
        _resourceBuilder = builder.AddProject<Projects.ReverseProxy>("ReverseProxy")
            .WithReplicas(replica)
            .WithExternalHttpEndpoints()
            .WithHttpEndpoint(name: "http", port: port, env: "PORT", isProxied: true);
    }
    
    private static string MakeServiceName(string name, int index) => $"services__{name}__http__{index}";
    
    public Configurator AddService(string route, IResourceBuilder<IResourceWithServiceDiscovery> resourceBuilder)
    {
        if (!_routeMap.TryGetValue(route, out List<string> list))
        {
            list = new List<string>();
        }
        list.Add(MakeServiceName(resourceBuilder.Resource.Name, 0));
        _routeMap[route] = list;
        _resourceBuilder.WithReference(resourceBuilder);
        return this;
    }
    
    public void Build()
    {
        _resourceBuilder.WithEnvironment("routes", "/");
        foreach (var route in _routeMap)
        {
            _resourceBuilder.WithEnvironment($"route:{route.Key}", string.Join(",", route.Value));
        }
    }
}