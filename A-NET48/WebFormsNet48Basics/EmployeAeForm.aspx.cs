using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WebFormsNet48Basics
{
    public partial class EmployeAeForm : BasePage
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["AlwaysEncryptedConnection"].ConnectionString; }
        }

        private int? EmployeId
        {
            get
            {
                int value;
                if (int.TryParse(Request.QueryString["id"], out value))
                {
                    return value;
                }

                return null;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (EmployeId.HasValue)
                {
                    litTitle.Text = "Edit Employe";
                    LoadEmploye(EmployeId.Value);
                }
                else
                {
                    litTitle.Text = "Create Employe";
                }
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            var nom = txtNom.Text.Trim();
            var prenom = txtPrenom.Text.Trim();

            if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom))
            {
                lblError.Text = "Nom and Prenom are required.";
                return;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    if (EmployeId.HasValue)
                    {
                        command.CommandText = "UPDATE Employes SET Nom = @Nom, Prenom = @Prenom WHERE Id = @Id";
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = EmployeId.Value;
                    }
                    else
                    {
                        command.CommandText = "INSERT INTO Employes (Nom, Prenom) VALUES (@Nom, @Prenom)";
                    }

                    command.Parameters.Add("@Nom", SqlDbType.NVarChar, 50).Value = nom;
                    command.Parameters.Add("@Prenom", SqlDbType.NVarChar, 50).Value = prenom;

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                Response.Redirect("~/EmployesAeList.aspx?message=Employe saved", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                lblError.Text = "Save failed. SQL error code: " + ex.Number;
            }
            catch
            {
                lblError.Text = "Save failed. Verify Always Encrypted connection settings and keys.";
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/EmployesAeList.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void LoadEmploye(int employeId)
        {
            lblError.Text = string.Empty;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT TOP 1 Id, Nom, Prenom FROM Employes WHERE Id = @Id";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = employeId;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            lblError.Text = "Employe not found.";
                            btnSave.Enabled = false;
                            return;
                        }

                        txtNom.Text = Convert.ToString(reader["Nom"]);
                        txtPrenom.Text = Convert.ToString(reader["Prenom"]);
                    }
                }
            }
            catch (SqlException ex)
            {
                lblError.Text = "Load failed. SQL error code: " + ex.Number;
                btnSave.Enabled = false;
            }
            catch
            {
                lblError.Text = "Load failed. Verify Always Encrypted connection settings and keys.";
                btnSave.Enabled = false;
            }
        }
    }
}
