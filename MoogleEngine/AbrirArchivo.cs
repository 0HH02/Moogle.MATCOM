namespace MoogleEngine;
using System.Diagnostics;

public class AbrirArchivo
{
    public static void Abrir(string archivo)
    {
        archivo = "D:/proyecto final/moogle-main/Content/" + archivo + ".txt";
        var process = new Process();
        process.StartInfo = new ProcessStartInfo()
        {
            WindowStyle = ProcessWindowStyle.Maximized,
            UseShellExecute = true,
            FileName = archivo,
        };
        process.Start();
    }
}