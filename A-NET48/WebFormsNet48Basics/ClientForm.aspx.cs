using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WebFormsNet48Basics
{
    public partial class ClientForm : BasePage
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["AdventureWorks2014Connection"].ConnectionString; }
        }

        private int? ClientId
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
                if (ClientId.HasValue)
                {
                    litTitle.Text = "Edit Client";
                    LoadClient(ClientId.Value);
                }
                else
                {
                    litTitle.Text = "Create Client";
                }
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            var nom = txtNom.Text.Trim();
            var numeroCb = txtNumeroCB.Text.Trim();

            if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(numeroCb))
            {
                lblError.Text = "Nom and value are required.";
                return;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    if (ClientId.HasValue)
                    {
                        command.CommandText =
                            "OPEN SYMMETRIC KEY Cle_Champs DECRYPTION BY CERTIFICATE Cert_Chiffrement; " +
                            "UPDATE Clients " +
                            "SET Nom = @Nom, NumeroCB = EncryptByKey(Key_GUID('Cle_Champs'), CONVERT(NVARCHAR(100), @NumeroCB)) " +
                            "WHERE Id = @Id; " +
                            "CLOSE SYMMETRIC KEY Cle_Champs;";
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = ClientId.Value;
                    }
                    else
                    {
                        command.CommandText =
                            "OPEN SYMMETRIC KEY Cle_Champs DECRYPTION BY CERTIFICATE Cert_Chiffrement; " +
                            "INSERT INTO Clients (Nom, NumeroCB) VALUES (@Nom, EncryptByKey(Key_GUID('Cle_Champs'), CONVERT(NVARCHAR(100), @NumeroCB))); " +
                            "CLOSE SYMMETRIC KEY Cle_Champs;";
                    }

                    command.Parameters.Add("@Nom", SqlDbType.NVarChar, 100).Value = nom;
                    command.Parameters.Add("@NumeroCB", SqlDbType.NVarChar, 100).Value = numeroCb;

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                Response.Redirect("~/ClientsList.aspx?message=Client saved", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch
            {
                lblError.Text = "Save failed. Verify SQL key/certificate/table configuration.";
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/ClientsList.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void LoadClient(int clientId)
        {
            lblError.Text = string.Empty;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "OPEN SYMMETRIC KEY Cle_Champs DECRYPTION BY CERTIFICATE Cert_Chiffrement; " +
                        "SELECT TOP 1 Id, Nom, CONVERT(NVARCHAR(100), DecryptByKey(NumeroCB)) AS NumeroCBDechiffre " +
                        "FROM Clients WHERE Id = @Id; " +
                        "CLOSE SYMMETRIC KEY Cle_Champs;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = clientId;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            lblError.Text = "Client not found.";
                            btnSave.Enabled = false;
                            return;
                        }

                        txtNom.Text = Convert.ToString(reader["Nom"]);
                        txtNumeroCB.Text = Convert.ToString(reader["NumeroCBDechiffre"]);
                    }
                }
            }
            catch
            {
                lblError.Text = "Load failed. Verify SQL key/certificate/table configuration.";
                btnSave.Enabled = false;
            }
        }
    }
}
