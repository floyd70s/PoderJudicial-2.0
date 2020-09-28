using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using PoderJudicial.Properties;

namespace PoderJudicial
{
    class Corte 
    {
        public int id;
        public string jurisdiccion;
        public string tribunal;
        public string modulo;
        public string fuentedocumento;
        public int loesid;
        public string codtribunalpj;

        public Corte()
        {
            this.id = 0;
            this.jurisdiccion = String.Empty;
            this.tribunal = String.Empty;
            this.modulo = String.Empty;
            this.fuentedocumento = String.Empty;
            this.loesid = 0;
            this.codtribunalpj = String.Empty;
        }

        //Traemos las cortes a las cuales no se ha descargado su archivo excel de exportacion
        public bool traer()
        {
                SQLiteConnection con = this.crearConneccion();
             try
               {
                List<Corte> cortes = new List<Corte>();
                string query = string.Empty;
                query = "select id, jurisdiccion, tribunal, modulo, fuentedocumento, loesid, codtribunalpj FROM tribunal where id = " + this.id;

                SQLiteCommand command = new SQLiteCommand(query, con);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    this.id = reader.GetInt32(0);
                    this.jurisdiccion = reader.GetString(1);
                    this.tribunal = reader.GetString(2);
                    this.modulo = reader.GetString(3);
                    this.fuentedocumento = reader.GetString(4);
                    this.loesid = reader.GetInt32(5);
                    this.codtribunalpj = reader.GetString(6);

                }
                reader.Close();
                con.Close();
                return true;
            }
            catch (Exception ex) {
                if (con.State != System.Data.ConnectionState.Closed)
                    con.Close();
                return false;
            }
        }

        //Traemos las cortes a las cuales no se ha descargado su archivo excel de exportacion
        public List<Corte> traerTodas(){

            SQLiteConnection con = this.crearConneccion();
            List<Corte> cortes = new List<Corte>();
            try
            {                
                string query = string.Empty;
                query = "select id, jurisdiccion, tribunal, modulo, fuentedocumento, loesid, codtribunalpj FROM tribunal order by id asc";

                SQLiteCommand command = new SQLiteCommand(query, con);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Corte corte = new Corte();
                    corte.id = reader.GetInt32(0);
                    corte.jurisdiccion = reader.GetString(1);
                    corte.tribunal = reader.GetString(2);
                    corte.modulo = reader.GetString(3);
                    corte.fuentedocumento = reader.GetString(4);
                    corte.loesid = reader.GetInt32(5);
                    corte.codtribunalpj = reader.GetString(6);
                    cortes.Add(corte);
                }
                reader.Close();
                con.Close();
            }
            catch (Exception ex) {
                if (con.State != System.Data.ConnectionState.Closed)
                    con.Close();
            }
            return cortes;        
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
       */
      public bool tienePlanillaDiaria(string fecha)
       {
          SQLiteConnection con = this.crearConneccion();
          int i = 0;

          try
          {
              List<Corte> cortes = new List<Corte>();
              string query = string.Empty;
              query = "select id from planilla where idtribunal = " + this.loesid.ToString() + " and fecha = '" + fecha + "'";

              SQLiteCommand command = new SQLiteCommand(query, con);
              SQLiteDataReader reader = command.ExecuteReader();
              
              while (reader.Read())
              {
                  i++;
              }
              reader.Close();
              con.Close();
          }
          catch (Exception ex) {
              if (con.State != System.Data.ConnectionState.Closed)
                  con.Close();
          }

        if (i > 0) return true;
        else return false;
       }
    }
}
