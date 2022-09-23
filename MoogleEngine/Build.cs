using System.Diagnostics;
using System.Text.Json;

public class Build
{
    public static string RetornarSnipet (string linea, string lineaUpper, string[] lineaArray, string palabra)
    {
                
                //Console.WriteLine(line[i]+"           "+ palabra);
                int inicio = lineaUpper.IndexOf(palabra);
                int posicioninicial = 0;
                int tamaño = linea.Length;
                //Console.WriteLine(linea);
                if (inicio<=150)
                {
                    posicioninicial = 0;
                }
                else
                {
                    posicioninicial = inicio-150;
                }
                if (linea.Length-(inicio+palabra.Length)<=150)
                {
                    tamaño = linea.Length-posicioninicial;
                }
                else
                {
                    tamaño = (inicio + palabra.Length + 150) - posicioninicial;
                }
                string snipet = linea.Substring(posicioninicial, tamaño);
                //string word = linea.Substring(lineaUpper.IndexOf(palabra), palabra.Length);
                //snipet = snipet.Replace(" "+ word, " <mark style=\"font-weight: bolder; background: white; padding: 0;\">" + word + "</mark> ");
        
        //string substring = linea;
        //Console.WriteLine(dvf);
        return snipet;
    }
    public static List <string> HacerLista(Dictionary<string, float[]> TF)
    {
        List <string> tf = new List<string>();
        tf = TF.Select(kvp => kvp.Key).ToList();  
        return tf;
    }

    /*public static Dictionary<string, float> HacerTFIDF (Dictionary <string, float[]> TF, float[] IDF, string[] carpeta)
    {
        Dictionary<string, float> TFIDF = new Dictionary<string, float>();
        int i =0;
        foreach (KeyValuePair<string, float[]> entry in TF)
        {
            float tfidf = 0;
            for (int j=0; j<carpeta.Length; j++)
            {
                tfidf = tfidf + (entry.Value[j] * IDF[i]);
            }
            //Console.WriteLine(IDF[i]);
            i++;
            TFIDF.Add(entry.Key, tfidf);
        }
        return TFIDF;
    }*/

    public static Dictionary<string, string[]> CargarSinonimos()
    {
       Dictionary<string, string[]> sinonimo = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText("../MoogleEngine/Synonymous.json"));
       return sinonimo;
    }

    public static float[] HacerIDF (Dictionary<string, float[]> TF, string[] carpeta)
    {
        float[] IDF = new float[TF.Count];
        //Calculamos la cantidad de veces que aparece la palabra en el documento
        int contador = 0;
        foreach (KeyValuePair<string, float[]> entry in TF)
        {
            for(int j=0; j<carpeta.Length; j++)
            {
                if(entry.Value[j]!=0)
                {
                    IDF[contador]++;
                }
            }
            contador++;
        }
        
        float CantDocumentos = carpeta.Length;

        double porcentaje = CantDocumentos*80/100;
        for (int i = 0; i < TF.Count; i++)
        {
            if (IDF[i]>porcentaje)
            {
                IDF[i]=0;
            }
            else 
            {
                IDF[i] = ((float) (CantDocumentos*10/(IDF[i]))-10);
            }            
        }
        //Console.WriteLine(string.Join(" ", IDF));
        return IDF;
    }

    public static (Dictionary<string, float[]>, Dictionary<string, string[]>)  HacerTF_BuscarSnipets (string[] carpeta)
    {
        Stopwatch time = new Stopwatch();
        time.Start();
        Dictionary<string, float[]> TF = new Dictionary<string, float[]>();
        Dictionary<string, string[]> Snipets = new Dictionary<string, string[]>();
        int[] CantPalabras = Rellenar(carpeta);
        for (int i=0; i<carpeta.Length; i++)
        {
            StreamReader documento = new StreamReader(carpeta[i]);
            string linea;
            //if (documento != null)
            //{
                //si no esta vacio empiezo a leer linea por linea hasta llegar al final
                do
                {
                    linea = documento.ReadLine();
                    //verifico si la linea esta vacia o no
                    if (linea != null)
                    {
                        string lineaUpper =linea.ToUpper();
                        string[] lineaArray = PicarString(lineaUpper);
                        for (int j=0; j<lineaArray.Length; j++)
                        {
                            string palabraLinea = lineaArray[j];
                            if (!TF.ContainsKey(palabraLinea))
                            {
                                float[] tf = new float[carpeta.Length];
                                string[] snipet = new string[carpeta.Length];
                                TF.Add(palabraLinea, tf);
                                Snipets.Add(palabraLinea, snipet);
                            }
                            TF[palabraLinea][i]++;
                            if (Snipets[palabraLinea][i]==null)
                            {
                                Snipets[palabraLinea][i] = RetornarSnipet(linea, lineaUpper, lineaArray, palabraLinea);
                            }
                        }
                        CantPalabras[i] = CantPalabras[i] + lineaArray.Length;
                    } 
                    
                }while (!documento.EndOfStream);
            Console.WriteLine(time.Elapsed + "   documento " + i);
        }

        TF = CalcularTF(CantPalabras, TF, carpeta);
        Console.WriteLine(time.Elapsed + "  Termino el TF y snipet");
        return (TF, Snipets);
    }
    public static int[] Rellenar (string[] carpeta)
    {
        int[] cantidad = new int[carpeta.Length];
        for(int i=0; i<carpeta.Length; i++)
        {
            cantidad[i]=0;
        }
        return cantidad;
    }
    public static Dictionary<string, float[]> CalcularTF(int[] cantPalabras, Dictionary<string, float[]> TF, string[] carpeta)
    {
        foreach (KeyValuePair<string, float[]> entry in TF)
        {
            //Console.WriteLine(i);
            for (int j=0; j<carpeta.Length; j++)
            {
                if (cantPalabras[j] == 0)
                    {
                        entry.Value[j] =0;
                    }
                else
                    {
                        entry.Value[j] = entry.Value[j]/cantPalabras[j];
                    }
            }
        }

        return TF;
    }
    
    public static string[] PicarString(string query)
    {
        string[] separadores = new string[] {" ", ",", ".", ":", ";", "(", ")", "[", "]", "?","<",">","/","+","-", "@","#","$","%", "&", "¿", "=","¡","_", "«", "»", "!", "~", "*", "^", "'"};
        string[] arrayQuery = query.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
        return arrayQuery;
    }

    /*public static Dictionary<string, string[]> CrearSnipetsArray (Dictionary<string, float[]> TF, string[] carpeta)
    {
        Dictionary<string, string[]> snipets = new Dictionary<string, string[]>();
        for (int i =0; i<TF.Count; i++)
        {
            string[] snipet = new string[TF.ElementAt(0).Value.Length];
            snipets.Add(TF.ElementAt(i).Key, snipet);
            for (int j=0; j<TF.ElementAt(0).Value.Length; j++)
            {
                if (TF[TF.ElementAt(i).Key][j]!=0)
                {
                    snipets[snipets.ElementAt(i).Key][j] = BuscarSnipet(carpeta[j], snipets.ElementAt(i).Key);
                }
            }
        }
        return snipets;
    }*/

    /*public static string BuscarSnipet (string documento, string palabra)
    {
        //Console.WriteLine(documento);
        StreamReader doc = new StreamReader(documento);
        string snipet = "";
        string linea;
            if (doc != null)
            {
                //si no esta vacio empiezo a leer linea por linea hasta llegar al final
                do
                {
                    linea = doc.ReadLine();
                    //verifico si la linea esta vacia o no
                    if (linea != null)
                    {
                        string[] lineaArray = PicarString(linea);
                        bool encontró = false;
                        for (int j=0; j<lineaArray.Length; j++)
                        {
                            if (lineaArray[j].ToUpper() == palabra.ToUpper())
                            {
                                snipet = RetornarSnipet(linea, palabra);
                                //Console.WriteLine(true);
                                encontró=true;
                                break;
                            }
                        }
                        if(encontró)
                        {
                            break;
                        }
                    } 
                }while (!doc.EndOfStream);
            }
            //Console.WriteLine(false);
        return snipet;
    }*/
}