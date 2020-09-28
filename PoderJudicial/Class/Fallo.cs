using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using PoderJudicial.Properties;

namespace PoderJudicial
{
    class Fallo
    {
        public string id; //idLoes       
        public string tribunal; //idTribunal
        public string rol; //Rol
        public string fecha; //Fechas
        public string partes; //Descripcion de partes
        public string parteActiva; //
        public string partePasiva; //
        public int numeroCaracteres; //Numero de caracteres del fallo
        public string archivo;  //Deshuso
        public string documentid;  //Deshuso
        public string texto; //Texto del fallo
        public bool registrado;

        public Fallo() {

            this.id = string.Empty;
            this.tribunal = string.Empty;
            this.rol = string.Empty;
            this.fecha = string.Empty;
            this.partes = string.Empty;
            this.parteActiva = string.Empty;
            this.partePasiva = string.Empty;
            this.numeroCaracteres = 0;
            this.archivo = string.Empty;
            this.documentid = string.Empty;
            this.texto = string.Empty;
            this.registrado = false;
        }


        public bool Existe() 
        {            
            string query = string.Empty;
            query = "Select id_fallo ";
            query+= "from fallojuris3 ";  
            query+= "where datediff(dd,falFecha,'" + this.formatoFecha() + "')=0 and ";
	        query+= "falTribunal1 = '" + this.tribunal.ToString() + "' and ";
	        query+= "falrol = '" + this.rol + "' ";

            SqlConnection con = new SqlConnection(Settings.Default.PJCS);
            con.Open();
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            int i = 0;
            while (reader.Read())
            {
                i++;
            }
            reader.Close();
            con.Close();

            if (i > 0)
                return true;
            return false;
        }

        /**
         * 
         * 
         */
        public bool Save(log4net.ILog log)
        {

            if (this.tribunal == "1") //Corte Suprema           
            { 
                if (!this.SaveCorteSuprema(log))
                    return false;
            }
            else //Corte Apelaciones
            {
                if (!this.SaveCorteApelaciones(log))
                    return false;
            }

            //Agregamos el sumario
            this.SaveSumario();

            //Agregamos las voces
            this.SaveVoces();

            return true;
        }

        /**
         * 
         * 
         */
        private void SaveVoces()
        {
            string query = string.Empty;
            query = "insert into SAEVozRelacionada(id_fallo, id_voz, descripcion) values(" + this.id.ToString() + ", 7352, 'SENTENCIA.ORIGINAL');";

            SqlConnection con = new SqlConnection(Settings.Default.PJCS);
            con.Open();
            SqlCommand command = new SqlCommand(query, con);
            command.ExecuteNonQuery();
            con.Close();
        }

        /**
         * 
         * 
         */
        private void SaveSumario()
        {
            string query = string.Empty;
            //Documento no analizado
            query = "insert into fallosumario(id_fallo, texto) values(" + this.id.ToString() + ", 'Sin referencia');";

            SqlConnection con = new SqlConnection(Settings.Default.PJCS);
            con.Open();
            SqlCommand command = new SqlCommand(query, con);
            command.ExecuteNonQuery();
            con.Close();
        }

        /**
         * 
         * 
         */
        private bool SaveCorteApelaciones(log4net.ILog log)
        {
            string query = string.Empty;
            query  = "insert into FalloJuris3 (";
            query += "   fal_IdUltEdic";
            query += "  ,fal_IdPenEdic";
            query += "  ,falFUltEdic";
            query += "  ,falFPenEdic";
            query += "  ,falEdiciones";
            query += "  ,falTribunal1";
            query += "  ,falRol";
            query += "  ,falSujA";
            query += "  ,falSujP";  
            query += "  ,falArea";
            query += "  ,falDescriptores";
            query += "  ,falSentencia";
            query += "  ,FalFecha";
            query += "  ,FalPublicar";
            query += "  ,FalAnalista";
            query += "  ,FalTerminado";
            query += "  ,falFechaCarga";
            query += "  ,falSentencia2";
            query += "  ,falTribunalBase";
            query += "  ,FalRol2";
            query += "  ,FalFecha2";
            query += "  ,Falorigen";
            query += "  ,FalLab";
            query += "  ,Fal_TriLaPrimera";
            query += "  ,FalRanking";
            query += "  ,FalApelacion";
            query += "  ,EsMasivo";
            query += ") VALUES (";
            query += "   1"; 
            query += "  ,1"; 
            query += "  ,getdate()"; 
            query += "  ,getdate()"; 
            query += "  ,0"; 
            query += "  ,@tribunal"; 
            query += "  ,@rol"; 
            query += "  ,@sujetoActivo"; 
            query += "  ,@sujetoPasivo"; 
            query += "  ,';1;'";
            query += "  ,'Resolución judicial'"; 
            query += "  ,@texto"; 
            query += "  ,@fecha"; 
            query += "  ,0"; 
            query += "  ,0"; 
            query += "  ,0"; 
            query += "  ,getdate()"; 
            query += "  ,@texto"; 
            query += "  ,0"; 
            query += "  ,@rol"; 
            query += "  ,@fecha";
            query += "  ,'JOL'";
            query += "  ,0";
            query += "  ,0";
            query += "  ,3";
            query += "  ,@tribunal";
            query += "  ,1";
            query += ") SELECT SCOPE_IDENTITY()";


            using (SqlConnection myConnection = new SqlConnection(Settings.Default.PJCS))
            {
                myConnection.Open();

                SqlCommand myCommand = new SqlCommand(query, myConnection);

                myCommand.Parameters.AddWithValue("@tribunal", this.tribunal);
                myCommand.Parameters.AddWithValue("@rol", this.rol);
                myCommand.Parameters.AddWithValue("@sujetoActivo", this.parteActiva);
                myCommand.Parameters.AddWithValue("@sujetoPasivo", this.partePasiva);
                myCommand.Parameters.AddWithValue("@texto", this.texto);
                myCommand.Parameters.AddWithValue("@fecha", this.formatoFechasSql());

                try
                {
                    this.id = Convert.ToInt32(myCommand.ExecuteScalar()).ToString();
                    myConnection.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Info("Error", ex);
                    if (myConnection.State == System.Data.ConnectionState.Open)
                        myConnection.Close();
                    return false;
                }
            }
        }

        /**
         * 
         * 
         */
        private bool SaveCorteSuprema(log4net.ILog log)
        {
            string query = string.Empty;
            query = "insert into FalloJuris3 (";
            query += "   fal_IdUltEdic";
            query += "  ,fal_IdPenEdic";
            query += "  ,falFUltEdic";
            query += "  ,falFPenEdic";
            query += "  ,falEdiciones";
            query += "  ,falTribunal1";
            query += "  ,falRol";
            query += "  ,falSujA";
            query += "  ,falSujP";
            query += "  ,falArea";
            query += "  ,falDescriptores";
            query += "  ,falSentencia";
            query += "  ,FalFecha";
            query += "  ,FalPublicar";
            query += "  ,FalAnalista";
            query += "  ,FalTerminado";
            query += "  ,falFechaCarga";
            query += "  ,falSentencia3";
            query += "  ,falTribunalBase";
            query += "  ,FalRol3";
            query += "  ,FalFecha3";
            query += "  ,Falorigen";
            query += "  ,FalLab";
            query += "  ,Fal_TriLaPrimera";
            query += "  ,FalRanking";
            query += "  ,EsMasivo";
            query += ") VALUES (";
            query += "   1";
            query += "  ,1";
            query += "  ,getdate()";
            query += "  ,getdate()";
            query += "  ,0";
            query += "  ,@tribunal";
            query += "  ,@rol";
            query += "  ,@sujetoActivo";
            query += "  ,@sujetoPasivo";
            query += "  ,';1;'";
            query += "  ,'Resolución judicial'";
            query += "  ,@texto";
            query += "  ,@fecha";
            query += "  ,0";
            query += "  ,0";
            query += "  ,0";
            query += "  ,getdate()";
            query += "  ,@texto";
            query += "  ,0";
            query += "  ,@rol";
            query += "  ,@fecha";
            query += "  ,'JOL'";
            query += "  ,0";
            query += "  ,0";
            query += "  ,3";
            query += "  ,1";
            query += ") SELECT SCOPE_IDENTITY()";

            using (SqlConnection myConnection = new SqlConnection(Settings.Default.PJCS))
            {
                myConnection.Open();

                SqlCommand myCommand = new SqlCommand(query, myConnection);

                myCommand.Parameters.AddWithValue("@tribunal", this.tribunal);
                myCommand.Parameters.AddWithValue("@rol", this.rol);
                myCommand.Parameters.AddWithValue("@sujetoActivo", this.parteActiva);
                myCommand.Parameters.AddWithValue("@sujetoPasivo", this.partePasiva);
                myCommand.Parameters.AddWithValue("@texto", this.texto);
                myCommand.Parameters.AddWithValue("@fecha", this.formatoFechasSql());

                try
                {
                    this.id = Convert.ToInt32(myCommand.ExecuteScalar()).ToString();
                    myConnection.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Info("Error", ex);
                    if (myConnection.State == System.Data.ConnectionState.Open)
                        myConnection.Close();                    

                    return false;
                }
            }
        }

        /**
         * yyyy-MM-dd HH:mm:ss
         */
        private object formatoFechasSql()
        {
            return this.fecha.Substring(6, 4) + "-" + this.fecha.Substring(3, 2) + "-" + this.fecha.Substring(0, 2) + " 00:00:00";
        }


        /**
         * 0123456789
         * 04/12/2019 => 2019-11-20 00:00:00.000
         */
        public string formatoFecha() {

            return this.fecha.Substring(6,4) + "-" + this.fecha.Substring(3,2) + "-" + this.fecha.Substring(0,2) + " 00:00:00.000";
        }

        internal int traerImportados(Corte c, string fecha)
        {
            try
            {
                string query = string.Empty;
                query = "SELECT count(*) FROM FalloJuris3 as f where f.esmasivo=1 and falTribunal1 = " + c.loesid.ToString() + " and convert(varchar, f.falFecha, 103) = '" + fecha +"'";

                SqlConnection con = new SqlConnection(Settings.Default.PJCS);
                con.Open();
                SqlCommand command = new SqlCommand(query, con);
                Int32 resultados = Convert.ToInt32(command.ExecuteScalar());                       
                con.Close();
                return resultados;
            }
            catch (Exception ex) {
                return 0;
            }
            
        }
    }
}
