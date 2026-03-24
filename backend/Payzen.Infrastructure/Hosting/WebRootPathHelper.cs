using Microsoft.AspNetCore.Hosting;

namespace Payzen.Infrastructure.Hosting;

/// <summary>
/// Garantit un chemin wwwroot utilisable : <see cref="IWebHostEnvironment.WebRootPath"/> peut être null
/// si le dossier wwwroot n'existe pas au démarrage, ce qui provoque ArgumentNullException dans Path.Combine.
/// </summary>
public static class WebRootPathHelper
{
    public static string ResolveWwwRoot(IWebHostEnvironment env)
    {
        ArgumentNullException.ThrowIfNull(env);

        var root = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(env.ContentRootPath, "wwwroot");

        Directory.CreateDirectory(root);
        return root;
    }
}
