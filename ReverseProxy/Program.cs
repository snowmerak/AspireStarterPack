using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

var routes = new List<RouteConfig>();

var injectedRoutes = Environment.GetEnvironmentVariable("routes")?.Split(',');
var injectedRouteMap = new Dictionary<string, List<string>>();
foreach (var route in injectedRoutes ?? Array.Empty<string>())
{
    var injectedServiceOfRoute = Environment.GetEnvironmentVariable($"route:{route}")?.Split(',');
    injectedRouteMap[route] = injectedServiceOfRoute?.ToList() ?? new List<string>();
}

var clusters = new List<ClusterConfig>();

foreach (var route in injectedRouteMap)
{
    var clusterId = $"cluster-{route.Key}";
    var destinations = new Dictionary<string, DestinationConfig>();
    for (var i = 0; i < route.Value.Count; i++)
    {
        var address = Environment.GetEnvironmentVariable($"{route.Value[i]}") ?? "http://127.0.0.1:8080";
        destinations[$"destination-{i}"] = new DestinationConfig
            { Address = address };
    }
    var cluster = new ClusterConfig()
    {
        ClusterId = clusterId,
        Destinations = destinations
    };

    clusters.Add(cluster);
    
    routes.Add(new RouteConfig()
    {
        RouteId = $"route-{route.Key}",
        ClusterId = clusterId,
        Match = new RouteMatch
        {
            Path = route.Key
        }
    });
}

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters);

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
Console.WriteLine($"ReverseProxy listening on: {port}");
    
var app = builder.Build();
app.MapReverseProxy();
app.Run(url: $"http://0.0.0.0:{port}");
