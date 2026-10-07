using BatallaNaval.Engine;
using BatallaNaval.Hubs;
using BatallaNaval.Services;

namespace BatallaNaval
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var commandArgs = args ?? Array.Empty<string>();

            if (commandArgs.Contains("--test"))
            {
                var engine = new GameEngine();
                engine.RunAutomatedSimulation();
                return;
            }

            var builder = WebApplication.CreateBuilder(commandArgs);

            builder.WebHost.UseUrls("http://localhost:5000");

            // Registrar Servicios
            builder.Services.AddSingleton<LeaderboardService>();
            builder.Services.AddControllers();
            builder.Services.AddSignalR();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllers();
            app.MapHub<BattleHub>("/hubs/battle");

            Console.WriteLine("\n==================================================================");
            Console.WriteLine(" ⚓ SERVIDOR DE BATALLA NAVAL (EDICIÓN MAESTRA) INICIADO ⚓");
            Console.WriteLine(" 🌐 Abre tu navegador en: http://localhost:5000");
            Console.WriteLine("==================================================================\n");

            app.Run();
        }
    }
}
