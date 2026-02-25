using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WebFormsNet48Basics
{
    public partial class ClientsList : BasePage
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["AdventureWorks2014Connection"].ConnectionString; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var message = Request.QueryString["message"];
                if (!string.IsNullOrWhiteSpace(message))
                {
                    lblMessage.Text = message;
                }

                BindClients();
            }
        }

        protected void gvClients_RowDeleting(object sender, System.Web.UI.WebControls.GridViewDeleteEventArgs e)
        {
            lblError.Text = string.Empty;

            var clientId = (int)gvClients.DataKeys[e.RowIndex].Value;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Clients WHERE Id = @Id";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = clientId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                lblMessage.Text = "Client deleted.";
            }
            catch
            {
                lblError.Text = "Delete failed.";
            }

            BindClients();
        }

        private void BindClients()
        {
            lblError.Text = string.Empty;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "OPEN SYMMETRIC KEY Cle_Champs DECRYPTION BY CERTIFICATE Cert_Chiffrement; " +
                        "SELECT Id, Nom, CONVERT(NVARCHAR(100), DecryptByKey(NumeroCB)) AS NumeroCBDechiffre " +
                        "FROM Clients ORDER BY Id; " +
                        "CLOSE SYMMETRIC KEY Cle_Champs;";

                    var table = new DataTable();
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }

                    gvClients.DataSource = table;
                    gvClients.DataBind();
                }
            }
            catch
            {
                lblError.Text = "Load failed. Verify SQL key/certificate/table configuration.";
            }
        }
    }
}
