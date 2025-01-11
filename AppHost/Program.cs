
var builder = DistributedApplication.CreateBuilder(args);

// var replicaSet = builder.AddContainerReplicaSet("redis",
//     (builder, name) => builder.AddContainer(name, "docker.io/redis", tag: "7.4.2").WithEndpoint(targetPort: 6379, name: "redis").WithEndpoint(targetPort: 16379, name: "cluster"),
//     interReplicaEndpoints: new []{"cluster"},
//     outboundReplicaEndpoints: new []{"redis", "cluster"},
//     replicaCount: 8);

var cache = builder.AddValkey(name: "SharedCache");
// replicaSet.InjectReferenceTo(cache);

var reverseProxy = builder.AddReverseProxy("ReverseProxy")
    .WithReplicas(2)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "http", port: 5000, env: "PORT", isProxied: true);


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