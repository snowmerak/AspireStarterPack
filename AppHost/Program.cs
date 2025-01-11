
var builder = DistributedApplication.CreateBuilder(args);

var replicaSet = builder.AddExecutableReplicaSet("grpc-echo-server",
    (builder, name) => builder.AddGolangApp(name, "../GrpcEchoServer").WithEndpoint(name: "grpc", env: "PORT"),
    outboundReplicaEndpoints: new []{"grpc"},
    replicas: 8);

var cache = builder.AddValkey(name: "SharedCache");

var reverseProxy = builder.AddReverseProxy("ReverseProxy")
    .WithReplicas(2)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "http", port: 5000, env: "PORT", isProxied: true);

for (var i = 0; i < 4; i++)
{
    var name = $"EchoServer-{i}";
    var echoServer = builder.AddGolangApp(name, "../EchoServer")
        .WithHttpEndpoint(env: "PORT");
    replicaSet.InjectReferenceTo(echoServer);
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