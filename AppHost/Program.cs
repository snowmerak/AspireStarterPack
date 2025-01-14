var builder = DistributedApplication.CreateBuilder(args);

var replicaSet = builder.AddReplicaSet<ExecutableResource>("grpc-echo-server",
    (builder, name) => builder.AddGolangApp(name, "../GrpcEchoServer")
        .WithEndpoint(name: "grpc", env: "GRPC_PORT")
        .WithHttpEndpoint(name: "healthz", env: "HEALTH_PORT")
        .WithHttpHealthCheck("/healthz", statusCode: 200, endpointName: "healthz"),
    outboundReplicaEndpoints: ["grpc"],
    replicas: 4);

var cache = builder.AddValkey(name: "SharedCache");

var reverseProxy = builder.AddReverseProxy("ReverseProxy")
    .WithReplicas(2)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "http", port: 5000, env: "PORT", isProxied: true);

for (var i = 0; i < 2; i++)
{
    var name = $"EchoServer-{i}";
    var echoServer = builder.AddGolangApp(name, "../EchoServer")
        .WithHttpEndpoint(env: "PORT");
    replicaSet.InjectReferenceTo(echoServer);
    reverseProxy.AddService("/echo", echoServer);
}

for (var i = 0; i < 3; i++)
{
    var name = $"CounterServer-{i}";
    var counterServer = builder.AddGolangApp(name, "../CounterServer")
        .WithHttpEndpoint(name: "http", env: "PORT")
        .WithHttpHealthCheck("/healthz", 200, "http")
        .WithReference(cache)
        .WaitFor(cache);
    reverseProxy.AddService("/count", counterServer);
}

// shutdown immediately all the services
foreach (var builderResource in builder.Resources)
{
    builderResource.
}

builder.Build().Run();