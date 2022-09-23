using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;
namespace MoogleServer;
public class Program
{
    /*public static string[] carpeta = Directory.GetFiles("../Content/", "*.txt");
    public static(Dictionary<string, float[]> TF, Dictionary <string, string[]> Snipets) TF_Snipet = Build.HacerTF_BuscarSnipets(carpeta);
    public static float[] IDF = Build.HacerIDF(TF_Snipet.TF, carpeta);
    public static List <string> tf= Build.HacerLista(TF_Snipet.TF);*/
    //public static Dictionary<string, float> TFIDF = Build.HacerTFIDF(TF_Snipet.TF, IDF, carpeta);
    //public static Dictionary<string, string[]> snipets = Build.Build.CrearSnipetsArray(TF, carpeta);
    public static void Main(string[] args)
    {
        Stopwatch time = new Stopwatch();
        time.Start();
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();

        app.MapFallbackToPage("/_Host");
        
        

        MoogleEngine.Moogle.TF_Snipet = Build.HacerTF_BuscarSnipets(MoogleEngine.Moogle.carpeta);
        MoogleEngine.Moogle.IDF = Build.HacerIDF(MoogleEngine.Moogle.TF_Snipet.TF, MoogleEngine.Moogle.carpeta);
        MoogleEngine.Moogle.tf = Build.HacerLista(MoogleEngine.Moogle.TF_Snipet.TF);
        MoogleEngine.Moogle.sinonimo = Build.CargarSinonimos();
        
        time.Stop();
        Console.WriteLine(time.Elapsed + " Terminó el build");
        app.Run();
    }
}