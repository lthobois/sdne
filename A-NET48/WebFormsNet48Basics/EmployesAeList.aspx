<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EmployesAeList.aspx.cs" Inherits="WebFormsNet48Basics.EmployesAeList" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>Employes AE - WebForms .NET 4.8</title>
    <link href="Content/site.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <h1>CRUD Employes (Always Encrypted)</h1>
        <p>
            <asp:HyperLink ID="lnkCreate" runat="server" NavigateUrl="~/EmployeAeForm.aspx">Creer Employe</asp:HyperLink>
            |
            <asp:HyperLink ID="lnkHome" runat="server" NavigateUrl="~/Default.aspx">Home</asp:HyperLink>
        </p>
        <asp:Label ID="lblMessage" runat="server" CssClass="text-success" />
        <asp:Label ID="lblError" runat="server" CssClass="text-error" />

        <asp:GridView ID="gvEmployes" runat="server" AutoGenerateColumns="False" DataKeyNames="Id"
            OnRowDeleting="gvEmployes_RowDeleting" EmptyDataText="No employee found.">
            <Columns>
                <asp:BoundField DataField="Id" HeaderText="Id" />
                <asp:BoundField DataField="Nom" HeaderText="Nom" />
                <asp:BoundField DataField="Prenom" HeaderText="Prenom (dechiffre cote client)" />
                <asp:TemplateField HeaderText="Actions">
                    <ItemTemplate>
                        <asp:HyperLink ID="lnkEdit" runat="server"
                            NavigateUrl='<%# Eval("Id", "~/EmployeAeForm.aspx?id={0}") %>'
                            Text="Edit" />
                        <asp:Button ID="btnDelete" runat="server" CommandName="Delete" Text="Delete" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </form>
</body>
</html>
