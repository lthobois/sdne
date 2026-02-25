<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EmployeAeForm.aspx.cs" Inherits="WebFormsNet48Basics.EmployeAeForm" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>Employe AE Form</title>
    <link href="Content/site.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <h1><asp:Literal ID="litTitle" runat="server" /></h1>
        <p>
            <asp:HyperLink ID="lnkList" runat="server" NavigateUrl="~/EmployesAeList.aspx">Back to list</asp:HyperLink>
        </p>

        <asp:Label ID="lblError" runat="server" CssClass="text-error" />

        <table>
            <tr>
                <td><label for="txtNom">Nom</label></td>
                <td><asp:TextBox ID="txtNom" runat="server" MaxLength="50" CssClass="w-420" /></td>
            </tr>
            <tr>
                <td><label for="txtPrenom">Prenom (colonne chiffree)</label></td>
                <td><asp:TextBox ID="txtPrenom" runat="server" MaxLength="50" CssClass="w-420" /></td>
            </tr>
        </table>

        <p>
            <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" />
            <asp:Button ID="btnCancel" runat="server" Text="Cancel" CausesValidation="false" OnClick="btnCancel_Click" />
        </p>
    </form>
</body>
</html>
