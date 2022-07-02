using Discord.InviteFilter.Data;
using Discord.InviteFilter.Services;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;

namespace Discord.InviteFilter;

class Program
{
    static Task Main(string[] args) =>
        CreateHostBuilder(args).Build().RunAsync();

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseConsoleLifetime()

            // Configure services
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton(s =>
                {
                    return new DiscordClient(new DiscordConfiguration
                    {
                        LoggerFactory = s.GetRequiredService<ILoggerFactory>(),
                        Token = hostContext.Configuration["Discord:Token"],
                        Intents = DiscordIntents.All
                    });
                })
                .AddSingleton(s => s.GetRequiredService<DiscordClient>().UseSlashCommands(new SlashCommandsConfiguration
                {
                    Services = s
                }))

                // Add services that depend on discord client
                .AddSingleton<InviteService>()

                // Database
                .AddDbContextFactory<ApplicationDbContext>(options =>
                {
                    string connString = hostContext.Configuration.GetConnectionString("Default");
                    options.UseMySql(connString, ServerVersion.AutoDetect(connString));
                })

                // Add hosted service
                .AddHostedService<BotService>();
            });
    }
}