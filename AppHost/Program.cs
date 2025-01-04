
var builder = DistributedApplication.CreateBuilder(args);

var reverseProxyConfigurator = new AppHost.ReverseProxy.Configurator(builder,
    replica: 4,
    port: 5000);

var cache = builder.AddValkey(name: "SharedCache");

for (var i = 0; i < 8; i++)
{
    var name = $"EchoServer-{i}";
    var echoServer = builder.AddGolangApp(name, "../EchoServer")
        .WithHttpEndpoint(env: "PORT");
    reverseProxyConfigurator.AddService("/echo", echoServer);
}

for (var i = 0; i < 8; i++)
{
    var name = $"CounterServer-{i}";
    var counterServer = builder.AddGolangApp(name, "../CounterServer")
        .WithHttpEndpoint(env: "PORT")
        .WithReference(cache);
    reverseProxyConfigurator.AddService("/counter", counterServer);
}

reverseProxyConfigurator.Build();

builder.Build().Run();