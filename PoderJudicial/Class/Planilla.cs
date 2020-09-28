using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using PoderJudicial.Properties;
using System.Data;
using System.IO;
using ExcelDataReader;
using System.Text.RegularExpressions;


namespace PoderJudicial
{
    class Planilla
    {
        public int id;
        public int idtribunal;
        public string fecha;
        public string ruta;
        public int procesar;

        public Planilla()
        {
            this.id = 0;
            this.idtribunal = 0;
            this.fecha = String.Empty;
            this.ruta = String.Empty;            
            this.procesar = 0;            
        }

        public SQLiteConnection crearConneccion()
        {

            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=" + Settings.Default.LocalDBPath + ";Version=3;New=True;Compress=True;");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

            }
            return sqlite_conn;
        }


        public bool Grabar() {

            SQLiteConnection con = this.crearConneccion();
            try
            {                
                List<Corte> cortes = new List<Corte>();
                string query = string.Empty;
                if (this.id == 0)
                    query = "INSERT INTO Planilla(idtribunal, fecha, ruta, procesar) VALUES(" + this.idtribunal.ToString() + ", '" + this.fecha + "', '" + this.ruta + "', 0);";
                else
                    query = "UPDATE Planilla SET procesar=" + this.procesar.ToString() + " WHERE id=" + this.id.ToString();
                SQLiteCommand command = new SQLiteCommand(query, con);
                command.ExecuteNonQuery();
                con.Close();

                return true;
            }
            catch (Exception ex)
            {
                if (con.State != System.Data.ConnectionState.Closed)
                    con.Close();

                return false;
            }
        }


        /**
         * 
         * 
         * 
         */
        public List<Planilla> traerSinProcesar()
        {
            SQLiteConnection con = this.crearConneccion();
            List<Planilla> planillas = new List<Planilla>();
            try
            {                
                string query = string.Empty;
                query = "select id, idtribunal, fecha, ruta, procesar FROM Planilla where procesar = 0 order by id asc";

                SQLiteCommand command = new SQLiteCommand(query, con);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Planilla planilla = new Planilla();
                    planilla.id = reader.GetInt32(0);
                    planilla.idtribunal = reader.GetInt32(1);
                    planilla.fecha = reader.GetString(2);
                    planilla.ruta = reader.GetString(3);
                    planilla.procesar = reader.GetInt32(4);
                    planillas.Add(planilla);
                }
                reader.Close();
                con.Close();
            }
            catch (Exception ex)
            {
                if (con.State != System.Data.ConnectionState.Closed)
                    con.Close();             
            }

            return planillas; 
        }

        /**
         * 
         * 
         */
        internal bool cargarExcel()
        {
           try{
               string path = Settings.Default.ExcelPath + "\\" + this.ruta;

               using (var reader = ExcelReaderFactory.CreateReader(File.OpenRead(path)))
               {
                   int i = 0;
                   while (reader.Read())
                   {
                       //reader.RowCount                       
                       if (i > 4)
                       {                           
                           Registro registro = new Registro();
                           
                           registro.idplanilla = this.id;
                           registro.idtribunal = this.idtribunal;

                           string strRol = reader.GetValue(1).ToString().Trim();
                           strRol = Regex.Replace(strRol, @"[^0-9-]", "");

                           registro.rol = strRol;
                           registro.fecha = this.fecha;
                           registro.partes = reader.GetValue(2).ToString().Trim();
                           registro.ndocumento = reader.GetValue(4).ToString().Trim();
                           registro.Save();                
                       }
                       i++;
                   }
               }

               return true;
            }catch(Exception ex){
                return false;
            }
        }
    }
}


