using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<ReverseProxy>("ReverseProxy")
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 3000, targetPort: 3000)
    .WithHttpsEndpoint(port: 3030, targetPort: 3030);

builder.Build().Run();