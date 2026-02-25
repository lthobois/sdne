<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebFormsNet48Basics.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>WebForms .NET 4.8</title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            Hello World - WebForms .NET 4.8
        </div>
        <p>
            <asp:HyperLink ID="lnkEmployees" runat="server" NavigateUrl="~/EmployeeList.aspx">
                Open Employee CRUD (Entity Framework + LINQ)
            </asp:HyperLink>
        </p>
        <p>
            <asp:HyperLink ID="lnkClientsCrypto" runat="server" NavigateUrl="~/ClientsList.aspx">
                Open Clients CRUD (SQL EncryptionByKey / DecryptByKey)
            </asp:HyperLink>
        </p>
        <p>
            <asp:HyperLink ID="lnkEmployesAe" runat="server" NavigateUrl="~/EmployesAeList.aspx">
                Open Employes CRUD (Always Encrypted)
            </asp:HyperLink>
        </p>
    </form>
</body>
</html>
