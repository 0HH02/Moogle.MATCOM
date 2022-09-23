using System.Text.Json;
using System.Diagnostics;
namespace MoogleEngine;


public static class Moogle
{
    public static string[] carpeta = Directory.GetFiles("../Content/", "*.txt");
    public static (Dictionary<string, float[]> TF, Dictionary<string, string[]> snipets) TF_Snipet = (new Dictionary<string, float[]>(), new Dictionary<string, string[]>());

    public static Dictionary<string, float[]> TF = new Dictionary <string,float[]>();
    public static List <string> tf = new List<string>();
    public static Dictionary<string, string[]> snipets = new Dictionary<string, string[]>();
    public static float[]IDF;
    public static Dictionary<string, string[]> sinonimo = new Dictionary<string, string[]>();
    


    public static SearchResult Query(string query) 
    { 
        Stopwatch time = new Stopwatch();
        time.Start();
        
        
        (string[] arrayQuery, int[,] operadores, string sugerencia) arrayQuery = PicarString(query, TF_Snipet.TF);
        float[] TFIDF_Sistema = HacerTFIDF_Sistema(IDF, arrayQuery.arrayQuery, arrayQuery.operadores, TF_Snipet.TF, carpeta, tf, sinonimo);
        string[] SnipetsTerminados = CargarSnipets(arrayQuery.arrayQuery, TF_Snipet.snipets,arrayQuery.operadores, IDF, tf, sinonimo);
        string[] titulo = CargarTitulos(carpeta);
        SearchItem[] busqueda = DevolverBusqueda(titulo, SnipetsTerminados, TFIDF_Sistema);
        time.Stop();
        Console.WriteLine(time.Elapsed);
        return new SearchResult (busqueda, arrayQuery.sugerencia);
        
    }
    public static (string, string[]) Sugerir (string[] array, Dictionary<string, float[]> TF, string query)
    {
        string sugerencia = query;
        string palabraSugerida = "";
        
        //RECORREMOS EL ARRAY DE PALABRAS DEL QUERY
        for (int i=0; i<array.Length; i++)
        {
            //OMITIMOS EL OPERADOR DE CERCANIA (SI APARECE)
            if (array[i]!="~")
            {
                //REVISAMOS SI LA PALABRA APARECE EN ALGUN DOCUMENTO
                if (!TF.ContainsKey(array[i].ToUpper()))
                {
                    //SI NO APARECE LE CALCULAMOS LA DISTANCIA DE LEVENSTEIN CON TODAS LAS PALABRAS DE LOS TEXTOS Y DEVOLVEMOS LA MAS PEQUEÑA
                    int distancia = int.MaxValue;
                    foreach (KeyValuePair<string, float[]> entry in TF)
                    {
                        //** PARA CALCULAR LA DISTANCIA DE LEVENSTEIN **//

                        //HACEMOS UNA MATRIZ BIDIMENSIONAL Y LLENAMOS LA PRIMERA FILA Y COLUMNA CON UNA SUCESION DE NUMEROS EMPEZANDO POR EL 0
                        int[,] matris = new int[array[i].Length+1,entry.Key.Length+1];
                        for (int j=0; j<array[i].Length+1;j++)
                        {
                            matris[j,0]=j;
                        }
                        for (int j=0; j<entry.Key.Length+1;j++)
                        {
                            matris[0,j]=j;
                        }

                        //RECORREMOS AMBAS PALABRAS Y CALCULAMOS LA DISTANCIA DE LEVENSTEIN
                        for (int j=1; j<=array[i].Length; j++)
                        {
                            for (int k=1; k<=entry.Key.Length; k++)
                            {
                                if (array[i].ToUpper()[j-1]==entry.Key[k-1])
                                {
                                    matris[j,k] = Math.Min(Math.Min(matris[j-1,k]+1, matris[j, k-1]+1), matris[j-1, k-1]);
                                }
                                else
                                {
                                    matris[j,k] = Math.Min(Math.Min(matris[j-1,k]+1, matris[j, k-1]+1), matris[j-1, k-1]+1);
                                }
                            }
                        } 
                        //DEVOLVEMOS LA MAS PEQUEÑA
                        if (distancia>matris[matris.GetLength(0)-1, matris.GetLength(1)-1])
                        {
                            distancia=matris[matris.GetLength(0)-1, matris.GetLength(1)-1];
                            palabraSugerida = entry.Key;
                        }
                    }
                    //Console.WriteLine(sugerencia);

                    sugerencia = sugerencia.Replace(array[i], palabraSugerida.ToLower());
                    
                }
            }
        }
        
        return (sugerencia.ToLower(), array);
    }

    public static SearchItem[] DevolverBusqueda(string[] titulo, string[] snipet, float[] TFIDF_Sistema)
    {

        //** DEVOLVEMOS UN OBJETO QUE CONTENDRÁ: EL NOMBRE DEL DOCUMENTO, EL SNIPET Y SU TFIDF, ORDENADOS DE MAYOR A MENOR SEGUN SUS TFIDF **//

    //  ======================================================================================================================================== 
   
        int[] OrdenSegunPosicion = OrdenarTFIDF(TFIDF_Sistema);
        SearchItem[] busqueda = new SearchItem[OrdenSegunPosicion.Length];
        for(int i=0; i<OrdenSegunPosicion.Length; i++)
        {
            busqueda[i]=new SearchItem(titulo[OrdenSegunPosicion[i]], snipet[OrdenSegunPosicion[i]], TFIDF_Sistema[i]);
        }
        return busqueda;
    }

    public static int[] OrdenarTFIDF (float[] TFIDF)
    {
        /*PARA ORDENAR EL TFIDF VAMOS A CREAR UN ARREGLO IGUAL AL ARREGLO DE LOS TFIDF, VAMOS A ORDENARLO DE MAYOR A MENOR,
        VAMOS A COMPARAR SUS POSICIONES Y A DEVOLVER UN NUEVO ARREGLO CON ESAS POSICIONES QUE NO CONTENGA LOS DOCUMENTOS
        CUYO TFIDF SEAN 0*/

        float[] orden = new float[TFIDF.Length];
        for(int j=0; j<TFIDF.Length; j++)
        {
            orden[j]=TFIDF[j];
        }
        Array.Sort(orden);
        Array.Reverse(orden);
        

        // CONTAMOS TODOS LOS RESULTADOS QUE NO SON 0
        int tamaño = orden.Length;
        for(int i=0; i<orden.Length; i++)
        {
            if (orden[i]==0)
            {
                tamaño = i;
                break;
            }
        }

        int[] final = new int[tamaño];
        for (int i=0; i<tamaño; i++)
        {
            for (int j=0; j<TFIDF.Length; j++)
            {
                if(orden[i]==TFIDF[j])
                {
                    final[i] = j;
                }
            }
        }
        
        return final;
    }

    public static string[] CargarTitulos(string[] carpeta)
    {
        //** HACEMOS UN ARREGLO CON LOS NOMBRES DE TODOS LOS DOCUMENTOS PERO SIN LA EXTENSION **//
        string[] nombres= new string[carpeta.Length];
        for (int i = 0; i < carpeta.Length; i++)
                {   
                    nombres[i] = Path.GetFileNameWithoutExtension(carpeta[i]);
                }
        return nombres;
    }

    public static string[] CargarSnipets(string[] query, Dictionary<string, string[]> snipets, int[,] operadores, float[] idf, List <string> tf, Dictionary<string, string[]> sinonimo)
    {
        //** DEVOLVEREMOS UN ARREGLO QUE CONTENDRÁ LOS SNIPETS PREVIAMENTE CARGADOS DE LAS PALABRAS MÁS RARAS DEL QEURY **//
        //EN CASO DE QUE EL DOCUMENTO NO TENGA NINGUNA PALABRA DEL QUERY DEVOLVEREMOS UN SNIPET QUE DICE: "NO HAY RESULTADOS"

        string[] snipet = new string[snipets.ElementAt(0).Value.Length];
        
        int[] orden = OrdenarPalabras(query, snipets, operadores, idf, tf);
        for (int i=0; i<snipets.ElementAt(0).Value.Length; i++)
        {
            for (int j=0; j<query.Length; j++)
            {
                string palabraQuery = query[orden[j]].ToUpper();
                
                if (snipets.ContainsKey(palabraQuery) && snipets[palabraQuery][i] != null)
                {
                    snipet[i]= snipets[palabraQuery][i];
                    string word = snipet[i].Substring(snipet[i].ToUpper().IndexOf(palabraQuery), palabraQuery.Length);
                    snipet[i] = snipet[i].Replace(word, "<mark style=\"font-weight: bolder; background: white; padding: 0;\">" + word + "</mark>");
                    break;
                }
                else if(sinonimo.ContainsKey(palabraQuery.ToLower()))
                {
                    string[] sinonimos = sinonimo[palabraQuery.ToLower()];
                    for (int z = 0; z < sinonimos.Length; z++)
                    {
                        //Console.WriteLine(snipets.ContainsKey(sinonimos[z].ToUpper()));
                        if(snipets.ContainsKey(sinonimos[z].ToUpper()) && snipets[sinonimos[z].ToUpper()][i] != null)
                        {
                            snipet[i]= snipets[sinonimos[z].ToUpper()][i];
                            string word = snipet[i].Substring(snipet[i].ToUpper().IndexOf(sinonimos[z].ToUpper()), sinonimos[z].Length);
                            snipet[i] = snipet[i].Replace(word, "<mark style=\"font-weight: bolder; background: white; padding: 0;\">" + word + "</mark>");
                            break;
                        }
                    }
                    break;
                }
            }
            if (snipet[i] == null)
            {
                snipet[i] = "No hay resultados";
            }
            
        }
        return snipet;
    }

    public static int[] OrdenarPalabras(string[] query, Dictionary<string, string[]> snipets, int[,] operadores,float[] idf,  List <string> tf)
    {
        float[] orden = new float[query.Length];
        for(int j=0; j<query.Length; j++)
        {
            //Console.WriteLine(true);
            if(snipets.ContainsKey(query[j].ToUpper()))
            {
                //Console.WriteLine(tf.IndexOf(query[j].ToUpper()));
                if (operadores[2,j]!=-1)
                {
                    
                   orden[j]=idf[tf.IndexOf(query[j].ToUpper())]* (float)Math.Pow(4,operadores[2,j]);
                    //Console.WriteLine(true);
                }
                else
                {
                    orden[j]=idf[tf.IndexOf(query[j].ToUpper())];
                }
            }
            else
            {
                orden[j]=0;
            }
        }

        // Console.WriteLine(idf[tf.IndexOf("TIERRA")]);
        // Console.WriteLine(idf[tf.IndexOf("DESDE")]);
        // Console.WriteLine(idf[tf.IndexOf("INVIERNO")]);
        /*for(int j=0; j<query.Length; j++)
        {
            //Console.WriteLine(true);
            if(snipets.ContainsKey(query[j].ToUpper()))
            {

                if (operadores[2,j]!=-1)
                {
                    TFIDF[query[j].ToUpper()] = TFIDF[query[j].ToUpper()] * (float)Math.Pow(4,operadores[2,j]);
                    //Console.WriteLine(true);
                }
                orden[j]=TFIDF[query[j].ToUpper()];
            }
            else
            {
                orden[j]=0;
            }
        }*/
        float[] aux = new float[query.Length];
        for (int i=0; i<query.Length; i++)
        {
            aux[i]=orden[i];
        }
        //Console.WriteLine(TFIDF["FE"]);
        //Console.WriteLine(string.Join(" ", orden));
        Array.Sort(orden);
        Array.Reverse(orden);
        //Console.WriteLine(string.Join(" ", orden));
        int[] final = new int[query.Length];
        for (int i=0; i<query.Length; i++)
        {
            for (int j=0; j<query.Length; j++)
            {
                if(orden[i]==aux[j])
                {
                    final[i] = j;
                }
            }
        }
        //Console.WriteLine(string.Join(" ", final));
        return final;
    }

    public static float[] HacerTFIDF_Sistema (float[] IDF, string[] query, int[,] operadores, Dictionary<string, float[]> TF, string[] carpeta, List <string> tf, Dictionary<string, string[]> sinonimo)
    {
        //** CALCULAMOS EL TFIDF DE CADA DOCUMENTO**//
        //PARA ELLO SUMAMOS LOS TFIDF (QUE OBTENEMOS MULTIPLICANDO EL TF Y EL IDF DE LA PALABRA) DE LAS PALABRAS DEL QUERY 
        // QUE ESTÉN PRESENTES EN CADA DOCUMENTO

    //  ================================================================================================================================

        
        float[] TFIDF_Sistema = new float[carpeta.Length];

        //POR CADA DOCUMENTO REVISAMOS SI CONTIENEN LAS PALABRAS DEL QUERY
        for (int j=0; j<carpeta.Length; j++)
        {
            for(int i=0; i<query.Length; i++)
            {
                if (TF.ContainsKey(query[i].ToUpper()))
                {
                    //VERIFICAMOS SI LA PALABRA ESTÁ EN CADA DOCUMENTO REVISANDO SI SU TF != 0 
                    if(TF[query[i].ToUpper()][j] != 0 )
                    {   

                        //SI LA PALABRA TENÍA EL OPERADOR: "!" DEVOLVEMOS UN TFIDF = 0 Y SALTAMOS A LA SIGUIENTE PALABRA
                        if  (operadores[0, i]==i)
                        {
                            TFIDF_Sistema[j] = 0;
                            break;
                        }

                        //SI LA PALABRA TENIA EL OPERADOR: "*" MULTIPLICAMOS SU TFIDF POR UNA FUNCION EXPONENCIAL DE BASE 4 Y EXPONENTE: LA CANTIDAD DE ASTERISCOS
                        else if (operadores[2, i]!=-1)
                        {
                            TFIDF_Sistema[j]= TFIDF_Sistema[j] + TF[query[i].ToUpper()][j]*IDF[tf.IndexOf(query[i].ToUpper())]*(float)Math.Pow(4,operadores[2,i]);
                        }

                        //SI LA PALABRA NO TENIA OPERADORES CALCULAMOS SU TFIDF
                        else
                        {
                        TFIDF_Sistema[j]= TFIDF_Sistema[j] + TF[query[i].ToUpper()][j]*IDF[tf.IndexOf(query[i].ToUpper())];
                        }
                    }
                    //SI LA PALABRA NO SE ENCUENTRA EN EL DOCUMENTO Y TIENE EL OPERADOR: "^" EL TFIDF DEL DOCUMENTO TAMBIÉN SERÁ 0 Y SALTAREMOS 
                    //HACIA OTRO DOCUMENTO
                    else if (operadores[1, i]==i)
                    {
                        TFIDF_Sistema[j] = 0;
                        break;
                    }
                    else if(sinonimo.ContainsKey(query[i].ToLower())){
                    string[] sinonimos = sinonimo[query[i].ToLower()];
                    for (int z = 0; z < sinonimos.Length; z++)
                        {
                            if(TF.ContainsKey(sinonimos[z].ToUpper()) && TF[sinonimos[z].ToUpper()][j] != 0)
                            {
                                TFIDF_Sistema[j]= TFIDF_Sistema[j] + TF[sinonimos[z].ToUpper()][j]*IDF[tf.IndexOf(sinonimos[z].ToUpper())]*0.01f;
                                break;
                            }
                        }
                    }
                }
                else if(sinonimo.ContainsKey(query[i].ToLower())){
                    string[] sinonimos = sinonimo[query[i].ToLower()];
                    for (int z = 0; z < sinonimos.Length; z++)
                    {
                        if(TF.ContainsKey(sinonimos[z].ToUpper()) && TF[sinonimos[z].ToUpper()][j] != 0)
                        {
                            TFIDF_Sistema[j]= TFIDF_Sistema[j] + TF[sinonimos[z].ToUpper()][j]*IDF[tf.IndexOf(sinonimos[z].ToUpper())]*0.01f;
                            break;
                        }
                    }
                }
            }
        }
        
        //REVISAMOS SI EL QUERY TIENE EL OPERADOR DE CERCANIA 
        for (int i = 0; i<query.Length; i++)
        {
            if (operadores[3, i]!=-1)
            {
                //** CALCULAMOS SU CERCANIA **//
                /* PARA ELLO MULTIPLICAMOS EL TFIDF DE TODOS LOS DOCUMENTOS POR UNA FUNCIÓN DE PROPORCIONALIDAD INVERSA PARA 
                QUE MIENTRAS MÁS CERCA ESTÉN LAS PALABRAS MÁS ALTO SEA SU TFIDF*/

            //  =============================================================================================================================

                //CREAMOS UN ARREGLOS DONDE GUARDAREMOS LA DISTANCIA ENTRE LAS PALABRAS EN CADA DOCUMENTO, SI SOLO APARECE UNA O NO APARECE
                //LA DISTANCIA TENDRÁ UN VALOR MAXIMO
                int[] cercania = new int[TFIDF_Sistema.Length]; 

                //VERIFICAMOS SI AMBAS PALABRAS ESTÁN EN EL MISMO DOCUMENTO
                for (int k=0; k<carpeta.Length; k++)
                {
                    string PrimeraPalabra = query[operadores[3,i]-1].ToUpper();
                    string SegundaPalabra = query[operadores[3,i]+1].ToUpper();
                    if (TF.ContainsKey(PrimeraPalabra) && TF[PrimeraPalabra][k] != 0)
                    {
                        if (TF.ContainsKey(SegundaPalabra) && TF[SegundaPalabra][k] != 0)
                        {

                            //SI CONTIENE A AMBAS PALABRAS BUSCAMOS EN EL DOCUMENTO CUAL ES LA MENOR DISTANCIA QUE HAY ENTRE ELLAS
                            cercania[k]= BuscarCercania(carpeta, PrimeraPalabra, SegundaPalabra, k);
                        }
                        else
                        {
                            cercania[k] = int.MaxValue;
                        }
                    }
                    else if (TF.ContainsKey(SegundaPalabra) && TF[SegundaPalabra][k] != 0)
                    {
                        cercania[k] = int.MaxValue;
                    }
                    else
                    {
                        cercania[k]=int.MaxValue;
                    }
                }
                
                //MULTIPLICAMOS EL TFIDF DE TODOS LOS DOCUMENTOS POR UNA FUNCIÓN DE PROPORCIONALIDAD INVERSA PARA QUE MIENTRAS MÁS CERCA
                //ESTÉN LAS PALABRAS MÁS ALTO SEA SU TFIDF
                for (int j=0; j<TFIDF_Sistema.Length; j++)
                {
                    if(cercania[j]!= int.MaxValue)
                    {
                        TFIDF_Sistema[j]= TFIDF_Sistema[j] * ((((float) 100)/cercania[j]) + 1);
                    }
                }
                break;
            }   
        }
        return TFIDF_Sistema;
    }

    public static int BuscarCercania(string[] carpeta, string Ppalabra, string Spalabra, int archivo)
    {
        int cercania = int.MaxValue;
        StreamReader documento= new StreamReader(carpeta[archivo]);
        string linea;
        int posicion=0;
        bool ppalabra = false;
        bool spalabra = false;
        int posicion1 = 0;
        int posicion2 = 0;
        
        do
        {
            linea = documento.ReadLine();
            //verifico si la linea esta vacia o no
            if (linea != null)
            {
                string[] lineaArray = PicarString(linea);
                for (int j=0; j<lineaArray.Length; j++)
                {
                    string palabraLinea = lineaArray[j].ToUpper();
                    posicion++;
                    if(palabraLinea==Ppalabra)
                    {
                        ppalabra = true;
                        posicion1 = posicion;
                        if (ppalabra && spalabra && (int) Math.Abs(posicion1-posicion2)<cercania)
                        {
                            //Console.WriteLine(true);
                            cercania = (int) Math.Abs(posicion1-posicion2);
                            spalabra = false;                            
                        }
                    }
                    else if (palabraLinea==Spalabra)
                    {
                        spalabra = true;
                        posicion2 = posicion;
                        if (ppalabra && spalabra && (int) Math.Abs(posicion1-posicion2)<cercania)
                        {
                            //Console.WriteLine(true);
                            cercania = (int) Math.Abs(posicion1-posicion2);
                            ppalabra = false;
                        }
                    }
                }
            } 
        }while (!documento.EndOfStream);
        //Console.WriteLine(cercania);
        return cercania;
    }

    public static (string[], int[,], string) PicarString(string query, Dictionary<string, float[]> TF)
    {
        //** DEVOLVEREMOS UN ARREGLO CON LAS PALABRAS DEL QUERY (QUE NO CONTENGA LOS OPERADORES), UNA MATRIZ CON LA INFORMACIÓN DE LOS OPERADORES Y LA SUGERENCIA **//

    //  ==============================================================================================================================================================
        string queryPersonal = query;
        if(queryPersonal.IndexOf("~")!=-1)
        {
            queryPersonal = queryPersonal.Replace("~", " ~ ");
        }

        string[] separadores = new string[] {" ", ",", ".", ":", ";", "(", ")", "[", "]", "?","<",">","/","+","-", "@","#","$","%", "&", "¿", "=","¡","_", "«", "»", "{", "}" };
        string[] arrayQuery = queryPersonal.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
        int[,] operadores = Rellenar(4, arrayQuery.Length);       

        /*HACEMOS UN MATRIZ EN LA QUE VOY A HACER CORRESPONDER: LAS FILAS CON EL TIPO DE OPERADOR QUE HAY EN EL QUERY
        Y LAS COLUMNAS CON LA POSICION EN LA QUE SE ENCUENTRA LA PALABRA QUE TIENE EL OPERADOR, EN EL CASO DE LOS 
        ASTERISCOS GUARDO LA CANTIDAD DE ASTERISCOS
        ----------
        |  0 | ! |        
        ----------
        |  1 | ^ |
        ----------
        |  2 | * |
        ----------
        |  3 | ~ |
        ----------
        
        */
        for(int i=0; i<arrayQuery.Length; i++)
        {
            switch(arrayQuery[i][0])
            {
                case '!': operadores[0, i] = i;
                arrayQuery[i]= arrayQuery[i].Substring(1, arrayQuery[i].Length-1);
                break;
                case '^': operadores[1, i] = i;
                arrayQuery[i]= arrayQuery[i].Substring(1, arrayQuery[i].Length-1);
                break;
                case '*': 
                int cantAsteriscos = 0;
                for (int j=0; j<arrayQuery[i].Length; j++)
                {
                    if(arrayQuery[i][j]=='*')
                    {
                        cantAsteriscos++;
                    }
                }
                
                operadores[2, i] = cantAsteriscos;

                //AL FINAL DE CADA CASE ELIMINAMOS LOS OPERADORES PARA DEVOLVER UN ARRAY CON LAS PALABRAS DE QUERY LIMPIO
                arrayQuery[i]= arrayQuery[i].Substring(cantAsteriscos, arrayQuery[i].Length-cantAsteriscos);
                break;
                case '~': 
                operadores[3, i] = i;
                if (operadores[3, i]==0 || operadores[3,i]==arrayQuery.Length-1)
                {
                    operadores[3, i] = -1;
                }
                break;
            }
        }
        //DEVOLVEMOS LA SUGERENCIA Y EL ARRAY DEL QUERY DEPURADO PARA SIGUIENTES OPERACIONES
        (string sugerencia, arrayQuery) = Sugerir(arrayQuery, TF, query);
        return (arrayQuery, operadores, sugerencia);
    }

    public static int[,] Rellenar (int filas, int columnas)
    {
        int[,] cantidad = new int[filas,columnas];
        for(int i=0; i<filas; i++)
        {
            for(int j=0; j<columnas; j++)
            cantidad[i, j]=-1;
        }
        return cantidad;
    }
    
    public static string[] PicarString(string query)
    {
        string[] separadores = new string[] { " ", ",", ".", ":", ";", "(", ")", "[", "]", "?","<",">","/","+","-"," - ", "@","#","$","%", "&", "¿", "=","¡","_", "«", "»" };
        string[] arrayQuery = query.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
        return arrayQuery;
    }
}