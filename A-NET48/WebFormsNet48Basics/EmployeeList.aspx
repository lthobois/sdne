<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EmployeeList.aspx.cs" Inherits="WebFormsNet48Basics.EmployeeList" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>Employees - WebForms .NET 4.8</title>
    <link href="Content/site.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <h1>HumanResources.Employee</h1>
        <p>
            <asp:HyperLink ID="lnkCreate" runat="server" NavigateUrl="~/EmployeeForm.aspx">Create Employee</asp:HyperLink>
            |
            <asp:HyperLink ID="lnkHome" runat="server" NavigateUrl="~/Default.aspx">Home</asp:HyperLink>
        </p>
        <asp:Label ID="lblMessage" runat="server" CssClass="text-success" />
        <asp:GridView ID="gvEmployees" runat="server" AutoGenerateColumns="False" DataKeyNames="BusinessEntityID"
            OnRowDeleting="gvEmployees_RowDeleting" EmptyDataText="No employee found.">
            <Columns>
                <asp:BoundField DataField="BusinessEntityID" HeaderText="BusinessEntityID" />
                <asp:BoundField DataField="NationalIDNumber" HeaderText="NationalIDNumber" />
                <asp:BoundField DataField="LoginID" HeaderText="LoginID" />
                <asp:BoundField DataField="JobTitle" HeaderText="JobTitle" />
                <asp:BoundField DataField="BirthDate" HeaderText="BirthDate" DataFormatString="{0:yyyy-MM-dd}" />
                <asp:BoundField DataField="HireDate" HeaderText="HireDate" DataFormatString="{0:yyyy-MM-dd}" />
                <asp:BoundField DataField="VacationHours" HeaderText="VacationHours" />
                <asp:BoundField DataField="SickLeaveHours" HeaderText="SickLeaveHours" />
                <asp:TemplateField HeaderText="Actions">
                    <ItemTemplate>
                        <asp:HyperLink ID="lnkEdit" runat="server"
                            NavigateUrl='<%# Eval("BusinessEntityID", "~/EmployeeForm.aspx?id={0}") %>'
                            Text="Edit" />
                        <asp:Button ID="btnDelete" runat="server" CommandName="Delete" Text="Delete" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </form>
</body>
</html>
