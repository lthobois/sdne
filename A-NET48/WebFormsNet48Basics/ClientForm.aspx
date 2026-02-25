<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ClientForm.aspx.cs" Inherits="WebFormsNet48Basics.ClientForm" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>Client Form - Chiffrement SQL</title>
    <link href="Content/site.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <h1><asp:Literal ID="litTitle" runat="server" /></h1>
        <p>
            <asp:HyperLink ID="lnkList" runat="server" NavigateUrl="~/ClientsList.aspx">Back to list</asp:HyperLink>
        </p>

        <asp:Label ID="lblError" runat="server" CssClass="text-error" />

        <table>
            <tr>
                <td><label for="txtNom">Nom</label></td>
                <td><asp:TextBox ID="txtNom" runat="server" MaxLength="100" CssClass="w-420" /></td>
            </tr>
            <tr>
                <td><label for="txtNumeroCB">Valeur a chiffrer</label></td>
                <td><asp:TextBox ID="txtNumeroCB" runat="server" MaxLength="100" CssClass="w-420" /></td>
            </tr>
        </table>

        <p>
            <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" />
            <asp:Button ID="btnCancel" runat="server" Text="Cancel" CausesValidation="false" OnClick="btnCancel_Click" />
        </p>
    </form>
</body>
</html>
