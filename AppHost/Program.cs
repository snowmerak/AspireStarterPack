using AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var reverseProxyConfigurator = new AppHost.ReverseProxy.Configurator(builder,
    replica: 8,
    port: 5000);

var cache = builder.AddValkey(name: "SharedCache");

var routeMap = new Dictionary<string, List<string>>();

for (var i = 0; i < 32; i++)
{
    var name = $"EchoServer-{i}";
    var echoServer = builder.AddGolangApp(name, "../EchoServer")
        .WithHttpEndpoint(env: "PORT")
        .WithReference(cache);
    reverseProxyConfigurator.AddService("/", echoServer);
}

reverseProxyConfigurator.Build();

builder.Build().Run();