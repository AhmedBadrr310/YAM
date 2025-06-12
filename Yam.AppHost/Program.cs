using YamlDotNet.Serialization;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Yam_AuthService>("yam-authservice");
builder.AddProject<Projects.Yam_NotificationService>("yam-notificationservice");

builder.AddProject<Projects.PostService>("postservice");

builder.AddProject<Projects.CommunityService>("communityservice");

builder.Build().Run();
