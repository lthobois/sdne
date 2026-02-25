using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WebFormsNet48Basics
{
    public partial class EmployesAeList : BasePage
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["AlwaysEncryptedConnection"].ConnectionString; }
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

                BindEmployes();
            }
        }

        protected void gvEmployes_RowDeleting(object sender, System.Web.UI.WebControls.GridViewDeleteEventArgs e)
        {
            lblError.Text = string.Empty;

            var employeId = (int)gvEmployes.DataKeys[e.RowIndex].Value;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Employes WHERE Id = @Id";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = employeId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                lblMessage.Text = "Employe deleted.";
            }
            catch (SqlException ex)
            {
                lblError.Text = "Delete failed. SQL error code: " + ex.Number;
            }
            catch
            {
                lblError.Text = "Delete failed.";
            }

            BindEmployes();
        }

        private void BindEmployes()
        {
            lblError.Text = string.Empty;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                using (var adapter = new SqlDataAdapter(command))
                {
                    command.CommandText = "SELECT Id, Nom, Prenom FROM Employes ORDER BY Id";

                    var table = new DataTable();
                    adapter.Fill(table);

                    gvEmployes.DataSource = table;
                    gvEmployes.DataBind();
                }
            }
            catch (SqlException ex)
            {
                lblError.Text = "Load failed. SQL error code: " + ex.Number;
            }
            catch
            {
                lblError.Text = "Load failed. Verify Always Encrypted connection settings and keys.";
            }
        }
    }
}
