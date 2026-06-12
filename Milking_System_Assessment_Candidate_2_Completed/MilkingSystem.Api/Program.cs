using Autofac;
using Autofac.Extensions.DependencyInjection;

using MilkingSystem.Core.Services;
using MilkingSystem.Core.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register DataService
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    containerBuilder.Register(c => new DataService(connectionString!))
        .AsSelf()
        .SingleInstance();

    containerBuilder.RegisterType<InMemoryRobotNotifier>()
        .As<IRobotNotifier>()
        .SingleInstance();
});

builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
