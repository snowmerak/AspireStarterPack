
var builder = DistributedApplication.CreateBuilder(args);

var reverseProxy = builder.AddReverseProxy("ReverseProxy")
    .WithReplicas(2)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "http", port: 5000, env: "PORT", isProxied: true);

var cache = builder.AddValkey(name: "SharedCache");

for (var i = 0; i < 4; i++)
{
    var name = $"EchoServer-{i}";
    var echoServer = builder.AddGolangApp(name, "../EchoServer")
        .WithHttpEndpoint(env: "PORT");
    reverseProxy.AddService("/echo", echoServer);
}

for (var i = 0; i < 4; i++)
{
    var name = $"CounterServer-{i}";
    var counterServer = builder.AddGolangApp(name, "../CounterServer")
        .WithHttpEndpoint(env: "PORT")
        .WithReference(cache)
        .WaitFor(cache);
    reverseProxy.AddService("/count", counterServer);
}

builder.Build().Run();