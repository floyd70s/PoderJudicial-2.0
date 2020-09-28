using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;
using PoderJudicial.Properties;
using System.Data.SqlClient;
using System.Net;
using System.Globalization;

namespace PoderJudicial
{
    /**
     * Para que el programa funcione sin problema debe constar con los siquientes programas:
     * -xpdf
     *  https://www.xpdfreader.com/
     *  Para la conversion de pdf a texto plano
     * 
     * -LibreOffice 
     *  https://es.libreoffice.org/descarga/libreoffice/
     *  Para la transformación de archivos desde doc a texto plano
     *  
     */
    class FirstTestCase
    {
       
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);      
            

        static void Main(string[] args)
        {

                log4net.Config.XmlConfigurator.Configure();
                string pathExcel = Settings.Default.ExcelPath;
                
                /**
                 * Vamos a extraer el estado diario
                 *   Rol
                 *   Partes
                 *   Fecha
                 *   Tribunal
                 *   Texto Completo
                 */

                //Folder donde ejecutar, validar argumentos
                //1.- Download Excels diarios
                //2.- Procesar Excels
                //3.- Procesar Cola
                string command = args[0];

                switch (command)
                {

                    //1.- Download Excels diarios
                    case "1": 
                                Corte corteInfo = new Corte();
                                List<Corte> cortes = corteInfo.traerTodas();
                                IFormatProvider culture = new CultureInfo("es-ES", true);
                                string fecha = DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy", culture);                                

                                foreach (Corte corte in cortes)
                                {                                   
                                    if (corte.tienePlanillaDiaria(fecha))
                                        continue;

                                    Console.WriteLine("===================================================================");
                                    Console.Write("Descargando planilla: " + corte.jurisdiccion);

                                    string docImportSrc = string.Empty;
                                    string excelDocPath = string.Empty;
                                    string fileName = string.Empty;

                                    try
                                    {
                                        //Tratamos de descargar el archivo
                                        using (WebClient webClient = new WebClient())
                                        {
                                            docImportSrc = "https://www.pjud.cl/estado-diario?p_p_id=estadodiario_WAR_estadodiarioportlet&p_p_lifecycle=2&p_p_state=normal&p_p_mode=view&p_p_cacheability=cacheLevelPage&p_p_col_id=column-3&p_p_col_pos=1&p_p_col_count=2&_estadodiario_WAR_estadodiarioportlet_campoTribunal=" + corte.codtribunalpj + "&_estadodiario_WAR_estadodiarioportlet_cur=1&tipoArchivo=EXCEL&fechaComun=" + fecha + "&campoSecretaria=&codTribunal=" + corte.codtribunalpj;
                                            fileName = corte.jurisdiccion + "-" + fecha.Replace("/", "") + ".xls";
                                            excelDocPath = @pathExcel + "\\" + fileName;
                                            webClient.DownloadFile(docImportSrc, @excelDocPath);
                                        }
                                        Console.WriteLine("........Ok");

                                    }
                                    catch (Exception ex)
                                    {
                                        FirstTestCase.regLog("[Fatal Error]\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ex.InnerException + "\r\n" + ex.Source);
                                        Console.WriteLine("........Fail");
                                        //si no logra descargar no registra nada en la db
                                        continue;
                                    }

                                    Console.WriteLine("-Registramos en la base de datos");

                                    Planilla planilla = new Planilla();
                                    planilla.idtribunal = corte.id;
                                    planilla.fecha = fecha;
                                    planilla.ruta = fileName;
                                    if (planilla.Grabar())
                                    {
                                        Console.WriteLine("Guardado...........................Ok");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Guardado.........................Fail");
                                    }

                        }
                        break;
                    //2.- Procesar Excels
                    case "2":
                        Planilla planillaInfo = new Planilla();
                        foreach (Planilla planilla in planillaInfo.traerSinProcesar()) { 
                              
                            //Procesamos el excel e insertamos en la tabla registro                                
                            if (planilla.cargarExcel()) {
                                planilla.procesar = 1;
                                planilla.Grabar();
                            }
                        }

                        break;
                    //3.- Procesar Cola
                    case "3":
                        Registro registroInfo = new Registro();
                        List<Registro> registrosCola = registroInfo.traerSinProcesar(Settings.Default.DocumentosLote);

                        //Limpiamos lod directorios 
                                               
                        //Principal pdf
                        FirstTestCase.cleanWorkingDir(Settings.Default.PDFPath);

                        Corte cortesList = new Corte();

                        //las carpetas de cada corte
                        foreach (Corte c in cortesList.traerTodas())
                            FirstTestCase.cleanWorkingDir(@Settings.Default.PathTxt + "\\" + c.loesid.ToString());


                        foreach (Registro r in registrosCola)
                        {
                            Corte corte = new Corte();
                            corte.id = r.idtribunal;
                            corte.traer();

                            try{
                                string fileName = string.Empty;
                                string pdfDocPath = string.Empty;

                                //Descargamos el archivo
                                using (WebClient webClient = new WebClient())
                                {
                                    //Antes de hacer la descarga verificamos que el documento no este registrado en la base de datos de manera de optimizar el proceso
                                    Fallo fallo = new Fallo();
                                    fallo.fecha = r.fecha;
                                    fallo.rol = r.rol;
                                    fallo.tribunal = corte.loesid.ToString();

                                    if (fallo.Existe()) {
                                        FirstTestCase.regLog("El fallo ROL: " + r.rol + ", del tribunal " + corte.jurisdiccion + ", y fecha " + fallo.fecha + ", ya se encuentra registrado.");
                                        continue;
                                    }

                                    string falloImportSrc = "https://www.pjud.cl/estado-diario?p_p_id=estadodiario_WAR_estadodiarioportlet&p_p_lifecycle=2&p_p_state=normal&p_p_mode=view&p_p_cacheability=cacheLevelPage&p_p_col_id=column-3&p_p_col_pos=1&p_p_col_count=2&_estadodiario_WAR_estadodiarioportlet_campoTribunal=" + corte.codtribunalpj + "&_estadodiario_WAR_estadodiarioportlet_cur=1&crr_documento=" + r.traerNumerodocumento() + "&tipoModulo=" + corte.modulo + "&fuenteDocumento=" + corte.fuentedocumento;
                                    fileName = r.rol + "-" + r.ndocumento.ToString() + ".pdf";
                                    pdfDocPath = @Settings.Default.PDFPath + "\\" + fileName;
                                    webClient.DownloadFile(falloImportSrc, pdfDocPath);
                                    FirstTestCase.regLog("El fallo ROL: " + r.rol + ", del tribunal " + corte.jurisdiccion + ", ha sido descargado");

                                    ConvertFiles(r, corte, fileName);
                                    r.procesado = 1;
                                    r.Save();

                                }
                            
                            }catch(Exception ex){
                                FirstTestCase.regLog("El fallo ROL: " + r.rol + ", del tribunal " + corte.jurisdiccion + ", No ha podido ser descargado");
                            }
                        }
                        break;

                    //3.- Procesar Cola
                    case "4":                        
                        FirstTestCase.enviarEmail();                        
                        break;
                }
        }

        /**
         * 
         * 
         */
        private static void ConvertFiles(Registro registro, Corte corte, string FileName)
        {
            bool procesar = false;
            string fileNameTxt = string.Empty;
            string path = Settings.Default.PDFPath;
            FileInfo file = new FileInfo(Settings.Default.PDFPath + "\\" + FileName);

            try
            {
                //Intentamos convertirlo en pdf
                FirstTestCase.regLog("Convirtiendo archivo:" + FileName);

                System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
                pProcess.StartInfo.FileName = @Settings.Default.PathToPdfToText;
                pProcess.StartInfo.Arguments = file.FullName;
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                pProcess.Start();
                Console.WriteLine(pProcess.StandardOutput.ReadToEnd()); //The output result
                pProcess.WaitForExit();

                fileNameTxt = file.FullName.Replace(file.Extension, "") + ".txt";
                procesar = true;
                FirstTestCase.regLog(FileName + ", Tranformado a PDF");

            }catch(Exception ex){
                try{
                    //Tratamos de convertirlo en doc
                    FirstTestCase.regLog("Convirtiendo archivo:" + file.Name);

                    System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
                    pProcess.StartInfo.FileName = @Settings.Default.PathToLibreOffice;
                    pProcess.StartInfo.Arguments = " --convert-to txt --outdir " + file.FullName;
                    pProcess.StartInfo.UseShellExecute = false;
                    pProcess.StartInfo.RedirectStandardOutput = true;
                    pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                    pProcess.Start();
                    Console.WriteLine(pProcess.StandardOutput.ReadToEnd()); //The output result
                    pProcess.WaitForExit();

                    fileNameTxt = file.FullName.Replace(file.Extension, "") + ".txt";
                    procesar = true;
                    FirstTestCase.regLog(FileName + ", Tranformado a DOC");

                }catch(Exception exp){
                    fileNameTxt = file.FullName.Replace(file.Extension, "") + ".txt";
                    file.CopyTo(fileNameTxt);
                    procesar = true;
                    FirstTestCase.regLog(FileName + ", Tranformado a txt");

                }
            }

            //Guardamos los datos en la base de datos
            if (File.Exists(fileNameTxt) && procesar == true)
            {
                try
                {
                    //creamos el Fallo
                    Fallo fallo = new Fallo();
                    fallo.tribunal = corte.loesid.ToString();
                    fallo.rol = registro.rol.Trim();
                    fallo.fecha = registro.fecha.Trim();
                    fallo.partes = registro.partes.Trim();

                    string fileNameTxtClean = @Settings.Default.PathTxt + "\\" + corte.loesid.ToString() + "\\" + fallo.rol + ".txt";
                    File.Copy(fileNameTxt, fileNameTxtClean, true);
                    FirstTestCase.regLog("Limpiando archivo (" + file.Name + ")" + fileNameTxtClean);
                    FirstTestCase.cleanFile(fileNameTxtClean, ref fallo);
                    FirstTestCase.saveAndValidate(fallo, corte);
                }
                catch (Exception ex)
                {
                    FirstTestCase.regLog(ex.Message + "\r\n" + ex.InnerException);
                    FirstTestCase.regLog("[ERROR] Linea 380, posiblemente copy, delete, clean o save and validate");
                }
            }
            else {
                FirstTestCase.regLog("[ERROR] El archivo " + fileNameTxt + ", no existe.");
            } 
            
        }

        /**
         * 
         * 
         */
        private static void saveAndValidate(Fallo fallo, Corte corte)
        {
            //Verificamos el numero de caracteres            
            int numeroCaracteresTexto = int.Parse(Settings.Default.NumeroCaracteres);
            if (fallo.numeroCaracteres < numeroCaracteresTexto)
            {
                FirstTestCase.regLog("[Info] El fallo rol: " + fallo.rol + ", emitido por el tribunal " + corte.jurisdiccion  + ", No cumple con el mínimo numero de caracteres (" + numeroCaracteresTexto + ").");
                return;
            }
            //Realizamos la verificacion fallo, rol, tribunal
            //Devuelve true si el fallo ya se encuentra registrado en la base de datos
            if (fallo.Existe())
            {
                FirstTestCase.regLog("[Info] El fallo rol: " + fallo.rol + ", emitido por el tribunal " + corte.jurisdiccion + ", Ya se encuentra registrado en la base de datos de acuerdo a su fecha, tribunal y rol.");
                return;
            }

            //Realizamos la verificacion de acuerdo a palabras claves en las partes
            string[] cortesExluir = new string[2] { "1", "8" };
            if (!fallo.partes.ToLower().Contains("isapre") && cortesExluir.Contains(fallo.tribunal))
            {
                FirstTestCase.regLog("El fallo ROL: " + fallo.rol + ", emitido por el tribunal " + corte.jurisdiccion + ",no contiene las palabras(Isapre) buscadas en sus partes [" + fallo.partes + "]");
                return;
            }


            //Si el fallo no existe lo insertamos en la base
            if (!fallo.Save(log))
            {
                FirstTestCase.regLog("[ERROR] No es posible grabar lo datos del fallo.");
            }
          
        }

        /**
         * 
         * 
         */
        private static void regLog(string message)
        {
            log.Info(message);
        }


        /**
         * Verificamos que no existan *.part en los archivos del directorio
         */
        private static void EsperarDescarga(string path)
        {
            for (;;)
            {

                System.IO.DirectoryInfo di = new DirectoryInfo(path);
                int parciales = 0;

                foreach (FileInfo file in di.GetFiles())
                {
                    if (file.FullName.Contains(".part"))
                    {
                        parciales++;
                    }
                }

                if (parciales > 0)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else break;
            }
            System.Threading.Thread.Sleep(2500);
        }

        /**
         *  
         */
        public static void cleanWorkingDir(string path)
        {

            System.IO.DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            /*
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            */

        }

        static string ReplaceHexadecimalSymbols(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, " ", RegexOptions.Compiled);
        }

        /**
         * 
         * 
         * 
         */
        public static void cleanFile(string filePath, ref Fallo fallo)            
        {

            string line;
            string fileText = string.Empty;
            FileInfo f = new FileInfo(filePath);
            long fileSize = f.Length;

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(filePath, Encoding.Default);                      
            while ((line = file.ReadLine()) != null)
            {
                string text = FirstTestCase.ReplaceHexadecimalSymbols(line.Trim());

                if (text.Trim() == string.Empty) continue;

                //Elimina codigo de barras
                if (text.Trim().Length == 10)
                { //PPNWKXXEXM       
                  //CWEJGYQKZH
                    if (text.ToUpper() != text)
                        fileText += FirstTestCase.finalPoint(line);
                }

                if (text.Trim().Length == 18)
                { 
                  //HXDCDFSXGVXLGCXGBB
                    if (text.ToUpper() != text)
                        fileText += FirstTestCase.finalPoint(line);
                }


                //Elimina numero de pagina
                else if (text.Trim().Length < 4)
                {

                    int numeroPagina = 0;
                    string texto = text.Trim();
                    if (!int.TryParse(texto, out numeroPagina))
                    {
                        fileText += FirstTestCase.finalPoint(texto);
                    }
                }
                else
                {
                    text = FirstTestCase.finalPoint(text);
                    text = FirstTestCase.doblePoint(text);
                    fileText += FirstTestCase.replaceCorreciones(text);
                }
            }

            /**
             * Procesamos las partes
             */            
            string[] stringSeparatorsCon = new string[] { " CON " };
            string[] arrPartes = fallo.partes.Split(stringSeparatorsCon, StringSplitOptions.None);
            string parteActiva = string.Empty;
            string partePasiva = string.Empty;

            if (arrPartes.Length == 2)
            { //Existen las partes
                parteActiva = arrPartes[0].Trim();
                partePasiva = arrPartes[1].Trim();
            }
            else {
                arrPartes = fallo.partes.Split("/".ToCharArray());
                if (arrPartes.Length == 2)
                { //Existen las partes
                    parteActiva = arrPartes[0].Trim();
                    partePasiva = arrPartes[1].Trim();
                }
                else { //CONTRA
                    string[] stringSeparatorsContra = new string[] { " CONTRA " };
                    arrPartes = fallo.partes.Split(stringSeparatorsContra, StringSplitOptions.None);
                    if (arrPartes.Length == 2)
                    { //Existen las partes
                        parteActiva = arrPartes[0].Trim();
                        partePasiva = arrPartes[1].Trim();
                    }
                }
            }

            fallo.parteActiva = parteActiva;
            fallo.partePasiva = partePasiva;
            fallo.texto = fileText.Trim();
            fallo.numeroCaracteres = fileText.Trim().Length;
            file.Close();          
        }

        /**
         * 
         * 
         */
        private static string replaceCorreciones(string texto)
        {
            IDictionary<string, string> replaces = new Dictionary<string, string>();

            replaces.Add("TENIE ND O", "TENIENDO");
            replaces.Add("CONSIDERACI ÓN", "CONSIDERACIÓN");
            replaces.Add("PRIME RO", "PRIMERO");
            replaces.Add("SEGU ND O", "SEGUNDO");
            replaces.Add("TERCE RO", "TERCERO");
            replaces.Add("NOVE NO", "NOVENO");
            replaces.Add("UND ÉC IMO", "UNDÉCIMO");
            replaces.Add("rechaz a", "rechaza");
            replaces.Add("Vistos:", "\r\n\r\nVistos:\r\n\r\n");
            replaces.Add("VISTOS:", "\r\n\r\nVISTOS:\r\n\r\n");
            replaces.Add("Visto:", "\r\n\r\nVisto:\r\n\r\n");
            replaces.Add("VISTO:", "\r\n\r\nVISTO:\r\n\r\n");
            replaces.Add("s éptimo:", "séptimo");
            replaces.Add("Vigés imo:", "Vigésimo");
            replaces.Add("acog e:", "acoge");
            replaces.Add("oct avo:", "oct avo");
            replaces.Add("Cuart o", "Cuarto");
            replaces.Add("Visto y teniendo presente:", "\r\n\r\nVisto y teniendo presente:\r\n\r\n");
            replaces.Add("Considerando:", "\r\n\r\nConsiderando:\r\n\r\n");
            replaces.Add("CONSIDERANDO:", "\r\n\r\nCONSIDERANDO:\r\n\r\n");
            replaces.Add("FdiDecFioEcChoDOdeCnoviembre:", "dieciocho de noviembre");            
        
           
            foreach (KeyValuePair<string, string> replace in replaces)
            {
                texto = texto.Replace(replace.Key, replace.Value);
            }

            return texto;
        }

        /**
         * 
         * 
         * 
         */
        private static string finalPoint(string text)
        {
            text = text.Trim();
            if (text.Substring(text.Length - 1, 1) == ".")
            {
                return text + FirstTestCase.saltoLinea();
            }
            else
            {
                return text + " ";
            }
        }

        /**
         * 
         * 
         */
        private static string saltoLinea()
        {
            return "\r\n\r\n";
        }

        /**
         * 
         * 
         * 
         */
        private static string doblePoint(string text)
        {
         
            int posicion = text.Trim().IndexOf(":");
            if (posicion == -1) //no Existe
                return text;
            else
            {
                if (posicion == text.Trim().Length - 1)
                    return text + FirstTestCase.saltoLinea();
                else
                    return text;
            }
        }

        /**
         * 
         * 
         */
        public static string fallosShema(string Rol, string parteActiva, string partePasiva, string fecha, string idCorte, string texto, long fileSize)
        {

            string code = "";

            code += "<fallo>\r\n";
            code += "\t<idCorte>" + idCorte + "</idCorte>\r\n";
            code += "\t<rol>" + Rol + "</rol>\r\n";
            code += "\t<fecha>" + fecha + "</fecha>\r\n";
            code += "\t<parteActiva>" + parteActiva + "</parteActiva>\r\n";
            code += "\t<partePasiva>" + partePasiva + "</partePasiva>\r\n";
            code += "\t<filesize>" + fileSize.ToString() + "</filesize>\r\n";
            code += "\t<texto>\r\n";
            code += texto;
            code += "</texto>\r\n";
            code += "</fallo>\r\n";

            return code;
        }

        /**
         * 
         * 
         * 
         * 
         */
        private static string getTemplateXML()
        {
            string text = string.Empty;

            System.IO.StreamReader sr = new System.IO.StreamReader(Settings.Default["EmailTemplate"].ToString(), System.Text.Encoding.Default);
            text = sr.ReadToEnd();
            sr.Close();

            return text;
        }

        /**
        * 
        * 
        */
        private static void enviarEmail()
        {
            string emailBody = FirstTestCase.getTemplateXML();
            string rowIndicador = string.Empty;
            IFormatProvider culture = new CultureInfo("es-ES", true);
            string fecha = DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy", culture);

            emailBody = emailBody.Replace("[##Fecha##]", DateTime.Now.ToString("dd.MM.yyyy"));

            Corte cortesList = new Corte();
            Registro registro = new Registro();
            Fallo fallo = new Fallo();
            emailBody = emailBody.Replace("[##Cola##]", registro.traerDocSinProcesar(fecha).ToString());

            int totalCola = 0;
            int totalProcesados = 0;
            int totalImportados = 0;


            //las carpetas de cada corte
            int i = 0;
            foreach (Corte c in cortesList.traerTodas())
            {
                int sinprocesar = registro.traerRegSinProcesar(c, fecha);
                totalCola += sinprocesar;

                int procesados = registro.traerProcesados(c, fecha);
                totalProcesados+= procesados;

                int importados = fallo.traerImportados(c, fecha);
                totalImportados += importados;

                rowIndicador += FirstTestCase.getRowTemplate(i++, fecha, c.jurisdiccion, c.loesid.ToString(), sinprocesar, procesados, importados);
            }

            rowIndicador += FirstTestCase.getRowTemplate(i++, fecha, "Total", "0", totalCola, totalProcesados, totalImportados);

            emailBody = emailBody.Replace("[##FALLOS##]", rowIndicador);
            FirstTestCase.grabarEmail(emailBody);

        }

        /***
        * 
        * 
        */
        private static string getRowTemplate(int i, string fecha, string titulo, string id ,int sinprocesar, int procesados, int grabados)
        {
            string color = "#FFF";
            string estatusTmp = string.Empty;

            if (i % 2 == 0)
                color = "EEE";
            else
                color = "FFF";

            string rTemplate = "<tr style=\"border-collapse:collapse; border:1px solid #666; background-color:#[##COLOR##];\">";
            rTemplate += "<td style=\"border-collapse:collapse; border:1px solid #666; padding:5px\" align=\"right\">";
            rTemplate += "<span style=\"color:#000000; font-size:11px; font-family:Arial;\">[##POS##]</span>";
            rTemplate += "</td>";
            rTemplate += "<td style=\"border-collapse:collapse; border:1px solid #666; padding:5px\" align=\"left\">";
            if(id!="0")
            rTemplate += "<span style=\"color:#000000; font-size:11px; font-family:Arial;\"><a href=\"http://bo.legalpublishing.cl/Intranet/Mant_Fallos/ListadoFallos.dev.asp?masivo=1&tribunal="+id+"&fecha="+fecha+"\">[##TRIBUNAL##]</a></span>";
            else
            rTemplate += "<span style=\"color:#000000; font-size:11px; font-family:Arial;\">[##TRIBUNAL##]</span>";
            rTemplate += "</td>";
            rTemplate += "<td style=\"border-collapse:collapse; border:1px solid #666; padding:5px\" align=\"right\">";
            rTemplate += "<span style=\"color:#000000; font-size:11px; font-family:Arial;\">[##SINPROCESAR##]</span>";
            rTemplate += "</td>";
            rTemplate += "<td style=\"border-collapse:collapse; border:1px solid #666; padding:5px\" align=\"right\">";
            rTemplate += "<span style=\"color:#000000; font-size:11px; font-family:Arial;\">[##PROCESADOS##]</span>";
            rTemplate += "</td>";
            rTemplate += "<td style=\"border-collapse:collapse; border:1px solid #666; padding:5px\" align=\"right\">";
            rTemplate += "<span style=\"color:#000000; font-size:11px; font-family:Arial;\">[##GRABADOS##]</span>";
            rTemplate += "</td>";
            rTemplate += "<td style=\"border-collapse:collapse; border:1px solid #666; padding:5px\" align=\"right\">";
            rTemplate += "<span style=\"color:#000000; font-size:11px; font-family:Arial;\">[##PORCENTAJE##]</span>";
            rTemplate += "</td>";
            rTemplate += "</tr>";

            rTemplate = rTemplate.Replace("[##COLOR##]", color);
            rTemplate = rTemplate.Replace("[##POS##]", (i + 1).ToString());
            rTemplate = rTemplate.Replace("[##TRIBUNAL##]", titulo);
            rTemplate = rTemplate.Replace("[##SINPROCESAR##]", sinprocesar.ToString());
            rTemplate = rTemplate.Replace("[##PROCESADOS##]", procesados.ToString());
            rTemplate = rTemplate.Replace("[##GRABADOS##]", grabados.ToString());
            rTemplate = rTemplate.Replace("[##PORCENTAJE##]", FirstTestCase.porcentaje(procesados, grabados));
            

            return rTemplate;
        }

        /**
         * Procesados -> 100
         * Grabados -> X
         * 
         * x = (Grabados * 100) / Procesados;
         * 
         */
        private static string porcentaje(int procesados, int grabados)
        {

            double dProcesados = double.Parse(procesados.ToString());
            double dGrabados = double.Parse(grabados.ToString());

            double percent = 0;
            if (procesados != 0)
            {
                percent = (dGrabados * 100) / dProcesados;
            }
            else {
                percent = 0;
            }

            return percent.ToString("n2");
        }
        
        /**
         * 
         * 
         * 
         */
        private static void grabarEmail(string msgHTML)
        {

            string cs = Settings.Default["EmailCS"].ToString();
            string DeNombre = Settings.Default["EmaildeNombre"].ToString();
            string DeCorreo = Settings.Default["EmaildeCorreo"].ToString();
            string Titulo = Settings.Default["EmailTitulo"].ToString();
            string ResponderA = Settings.Default["EmailResponderA"].ToString();
            string ParaCC = Settings.Default["EmailParaCC"].ToString();
            string ContentHTML = "1";

            string EmailNombre = Settings.Default["EmailNombre"].ToString();
            string EmailEmail = Settings.Default["EmailEmail"].ToString();

            string query = "INSERT INTO ErrMail(ErrNombreDe, ErrMailDe, ErrNombrePara, ErrMailPara, ErrTitulo, ErrMensaje, ErrResponderA, ErrParaCC,ErrTipoMensaje, ErrFechaError, ErrFechaEnviado, ErrEnviado, ErrNroIntento) ";
            query += " VALUES('" + DeNombre + "',  '" + DeCorreo + "', '" + EmailNombre + "', '" + EmailEmail + "', '" + Titulo + "', '" + msgHTML + "', '" + ResponderA + "', '" + ParaCC + "', " + ContentHTML + ", GETDATE(), NULL, 0, 0 )";

            SqlConnection con = new SqlConnection(cs);
            try
            {                
                con.Open();
                SqlCommand command = new SqlCommand(query, con);
                command.ExecuteNonQuery();
                con.Close();

                FirstTestCase.regLog("[Info] Los correos electrónicos han sido despechados");
                FirstTestCase.regLog("[Info] Correo electrónico");
                FirstTestCase.regLog(msgHTML);
                FirstTestCase.regLog("==================================================== ");
                

            }
            catch (Exception ex)
            {
                if (con.State != System.Data.ConnectionState.Closed)
                    con.Close();

                FirstTestCase.regLog("[Error] El envio de correos a presentado problemas");
            }
        }
    }
}
