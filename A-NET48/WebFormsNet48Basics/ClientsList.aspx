<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ClientsList.aspx.cs" Inherits="WebFormsNet48Basics.ClientsList" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>Clients Chiffres - WebForms .NET 4.8</title>
    <link href="Content/site.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <h1>CRUD Clients (chiffrement SQL)</h1>
        <p>
            <asp:HyperLink ID="lnkCreate" runat="server" NavigateUrl="~/ClientForm.aspx">Creer Client</asp:HyperLink>
            |
            <asp:HyperLink ID="lnkHome" runat="server" NavigateUrl="~/Default.aspx">Home</asp:HyperLink>
        </p>
        <asp:Label ID="lblMessage" runat="server" CssClass="text-success" />
        <asp:Label ID="lblError" runat="server" CssClass="text-error" />

        <asp:GridView ID="gvClients" runat="server" AutoGenerateColumns="False" DataKeyNames="Id"
            OnRowDeleting="gvClients_RowDeleting" EmptyDataText="No client found.">
            <Columns>
                <asp:BoundField DataField="Id" HeaderText="Id" />
                <asp:BoundField DataField="Nom" HeaderText="Nom" />
                <asp:BoundField DataField="NumeroCBDechiffre" HeaderText="Valeur dechiffree" />
                <asp:TemplateField HeaderText="Actions">
                    <ItemTemplate>
                        <asp:HyperLink ID="lnkEdit" runat="server"
                            NavigateUrl='<%# Eval("Id", "~/ClientForm.aspx?id={0}") %>'
                            Text="Edit" />
                        <asp:Button ID="btnDelete" runat="server" CommandName="Delete" Text="Delete" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </form>
</body>
</html>
