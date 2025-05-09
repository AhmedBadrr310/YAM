using YamlDotNet.Serialization;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Yam_AuthService>("yam-authservice");
builder.AddProject<Projects.Yam_NotificationService>("yam-notificationservice");

builder.Build().Run();
