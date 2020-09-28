using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using PoderJudicial.Properties;

namespace PoderJudicial
{
    class Registro
    {
        public int id;
        public int idplanilla;
        public int idtribunal;
        public string rol;
        public string fecha;
        public string partes;
        public string ndocumento;
        public string fechahoraregistro;
        public int procesado;

        public Registro()
        {
            this.id = 0;
            this.idplanilla = 0;
            this.idtribunal = 0;
            this.rol = string.Empty;
            this.fecha = string.Empty;
            this.partes = string.Empty;
            this.ndocumento = string.Empty;
            this.fechahoraregistro = string.Empty;
            this.procesado = 0;
        }

        //Traemos las cortes a las cuales no se ha descargado su archivo excel de exportacion
        public bool traer()
        {
            SQLiteConnection con = this.crearConneccion();
            try
            {                
                List<Corte> cortes = new List<Corte>();
                string query = string.Empty;
                query = "SELECT id, idplanilla, idtribunal, rol, fecha, partes, ndocumento, fechahoraregistro, procesado FROM Registro where id = " + this.id;

                SQLiteCommand command = new SQLiteCommand(query, con);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    this.id = reader.GetInt32(0);
                    this.idplanilla = reader.GetInt32(1);
                    this.idtribunal = reader.GetInt32(2);
                    this.rol = reader.GetString(3);
                    this.fecha = reader.GetString(4);
                    this.partes = reader.GetString(5);
                    this.ndocumento = reader.GetString(6);
                    this.procesado = reader.GetInt32(8);

                }
                reader.Close();
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

        //Traemos las cortes a las cuales no se ha descargado su archivo excel de exportacion
        public List<Registro> traerSinProcesar(string limite){

            SQLiteConnection con = this.crearConneccion();
            List<Registro> registros = new List<Registro>();
            try{
                string query = string.Empty;
                query = "select id, idplanilla, idtribunal, rol, fecha, partes, ndocumento, fechahoraregistro, procesado  from registro where procesado = 0 order by id asc limit " + limite;

                SQLiteCommand command = new SQLiteCommand(query, con);
                SQLiteDataReader reader = command.ExecuteReader();            
                while (reader.Read())
                {
                    Registro registro = new Registro();
                    registro.id = reader.GetInt32(0);
                    registro.idplanilla = reader.GetInt32(1);
                    registro.idtribunal = reader.GetInt32(2);
                    registro.rol = reader.GetString(3);
                    registro.fecha = reader.GetString(4);
                    registro.partes = reader.GetString(5);
                    registro.ndocumento = reader.GetString(6);
                    registro.procesado = reader.GetInt32(8);
                    registros.Add(registro);
                }
                reader.Close();
                con.Close();
            }
            catch (Exception ex)
            {
                if (con.State != System.Data.ConnectionState.Closed)
                    con.Close();             
            }
            return registros;        
        }


       public SQLiteConnection crearConneccion()
        {
 
         SQLiteConnection sqlite_conn;
         // Create a new database connection:
         sqlite_conn = new SQLiteConnection("Data Source=" + Settings.Default.LocalDBPath  + ";Version=3;New=True;Compress=True;");
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


       /**
        * 
        * 
        * 
        */
       public bool Save()
       {
          SQLiteConnection con = this.crearConneccion();

          try
          {              
              List<Corte> cortes = new List<Corte>();
              string query = string.Empty;
              if(this.id==0)
                query = "INSERT INTO Registro(idplanilla, idtribunal, rol, fecha, partes, ndocumento) VALUES(" + this.idplanilla + ", " + this.idtribunal + ", '" + this.rol + "', '" + this.fecha + "', '" + this.partes + "', '" + this.ndocumento + "');";
              else
                  query = "UPDATE Registro SET procesado = " + this.procesado.ToString() + " where id = " + this.id.ToString();

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
        * El numero de documento puede estar compuesto por varios documentos, solo cargamos el primero
        * Ej. 3915517;3915518
        */
       internal string traerNumerodocumento()
       {
           string[] documentos = this.ndocumento.Split(";".ToCharArray());
           if (documentos.Length > 0)
               return documentos[0];
           else return string.Empty;
       }

       /**
        * 
        * 
        */
       internal Int32 traerProcesados(Corte corte, string fecha)
       {
           SQLiteConnection con = this.crearConneccion();
           try
           {
               List<Corte> cortes = new List<Corte>();
               string query = string.Empty;
               query = "select count(*) from registro where fecha = '" + fecha + "' and idtribunal = " + corte.id + " and procesado = 1";

               SQLiteCommand command = new SQLiteCommand(query, con);
               Int32 registros = Convert.ToInt32(command.ExecuteScalar());               
               con.Close();
               return registros;
           }
           catch (Exception ex)
           {
               if (con.State != System.Data.ConnectionState.Closed)
                   con.Close();

               return 0;
           }
       }

       /**
        * 
        * 
        */
       internal object traerDocSinProcesar(string fecha)
       {
           SQLiteConnection con = this.crearConneccion();
           try
           {
               List<Corte> cortes = new List<Corte>();
               string query = string.Empty;
               query = "select count(*) from registro where fecha = '" + fecha + "' and procesado = 0";

               SQLiteCommand command = new SQLiteCommand(query, con);
               Int32 registros = Convert.ToInt32(command.ExecuteScalar());
               con.Close();
               return registros;
           }
           catch (Exception ex)
           {
               if (con.State != System.Data.ConnectionState.Closed)
                   con.Close();

               return 0;
           }
       }

       /**
        * 
        * 
        */
       internal int traerRegSinProcesar(Corte corte, string fecha)
       {
           SQLiteConnection con = this.crearConneccion();
           try
           {               
               List<Corte> cortes = new List<Corte>();
               string query = string.Empty;
               query = "select count(*) from registro where fecha = '" + fecha + "' and idtribunal = " + corte.id + " and procesado = 0";

               SQLiteCommand command = new SQLiteCommand(query, con);
               Int32 registros = Convert.ToInt32(command.ExecuteScalar());
               con.Close();
               return registros;
           }
           catch (Exception ex)
           {
               if (con.State != System.Data.ConnectionState.Closed)
                   con.Close();

               return 0;
           }
       }
    }
}
