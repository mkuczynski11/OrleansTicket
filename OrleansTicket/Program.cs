using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering(siloPort: 11112, gatewayPort: 30001);
    siloBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "Cluster1";
        options.ServiceId = "Ticketing";
    });
    // Configure silo to work on cluster with postgresql database
    siloBuilder.UseAdoNetClustering(options =>
    {
        // RUN THIS https://github.com/dotnet/orleans/blob/main/src/AdoNet/Shared/PostgreSQL-Main.sql
        // RUN THIS https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Clustering.AdoNet/PostgreSQL-Clustering.sql
        // RUN THIS https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Clustering.AdoNet/Migrations/PostgreSQL-Clustering-3.7.0.sql
        options.Invariant = "Npgsql";
        options.ConnectionString = "Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=admin;";
    });
    //// Option to set up storage for grain without database
    //siloBuilder.AddMemoryGrainStorage("users");
    // Option to set up storage for grain with postgresql connection on localhost:5432 for user=postgres and password=admin to db=postgres
    // You must create database and run 2 scripts provided below in order to initialize db proprely for Orleans
    siloBuilder.AddAdoNetGrainStorage("users", options =>
    {
        // RUN THIS https://github.com/dotnet/orleans/blob/main/src/AdoNet/Shared/PostgreSQL-Main.sql
        // RUN THIS https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Persistence.AdoNet/PostgreSQL-Persistence.sql
        options.Invariant = "Npgsql";
        options.ConnectionString = "Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=admin;";
    });
    siloBuilder.UseDashboard(options => { });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
