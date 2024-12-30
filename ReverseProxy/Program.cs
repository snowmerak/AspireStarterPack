using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

var routes = new List<RouteConfig>
{
    
};

var clusters = new List<ClusterConfig>
{
    
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters);
    
var app = builder.Build();
app.MapReverseProxy();
app.Run();
