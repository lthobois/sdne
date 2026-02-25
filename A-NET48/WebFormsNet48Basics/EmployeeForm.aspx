<%@ Page Language="C#" AutoEventWireup="true" ValidateRequest="false" CodeBehind="EmployeeForm.aspx.cs" Inherits="WebFormsNet48Basics.EmployeeForm" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>Employee Form - WebForms .NET 4.8</title>
    <link href="Content/site.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <h1><asp:Literal ID="litTitle" runat="server" /></h1>
        <p>
            <asp:HyperLink ID="lnkList" runat="server" NavigateUrl="~/EmployeeList.aspx">Back to list</asp:HyperLink>
        </p>
        <asp:Label ID="lblError" runat="server" CssClass="text-error" />
        <table>
            <tr>
                <td><label for="txtBusinessEntityID">BusinessEntityID</label></td>
                <td>
                    <asp:TextBox ID="txtBusinessEntityID" runat="server" />
                </td>
            </tr>
            <tr>
                <td><label for="txtNationalIDNumber">NationalIDNumber</label></td>
                <td>
                    <asp:TextBox ID="txtNationalIDNumber" runat="server" MaxLength="15" />
                </td>
            </tr>
            <tr>
                <td><label for="txtLoginID">LoginID</label></td>
                <td>
                    <asp:TextBox ID="txtLoginID" runat="server" MaxLength="256" CssClass="w-420" />
                </td>
            </tr>
            <tr>
                <td><label for="txtJobTitle">JobTitle</label></td>
                <td>
                    <asp:TextBox ID="txtJobTitle" runat="server" MaxLength="50" CssClass="w-280" />
                </td>
            </tr>
            <tr>
                <td><label for="txtBirthDate">BirthDate (yyyy-MM-dd)</label></td>
                <td>
                    <asp:TextBox ID="txtBirthDate" runat="server" />
                </td>
            </tr>
            <tr>
                <td><label for="ddlMaritalStatus">MaritalStatus</label></td>
                <td>
                    <asp:DropDownList ID="ddlMaritalStatus" runat="server">
                        <asp:ListItem Value="S">S - Single</asp:ListItem>
                        <asp:ListItem Value="M">M - Married</asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td><label for="ddlGender">Gender</label></td>
                <td>
                    <asp:DropDownList ID="ddlGender" runat="server">
                        <asp:ListItem Value="M">M - Male</asp:ListItem>
                        <asp:ListItem Value="F">F - Female</asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td><label for="txtHireDate">HireDate (yyyy-MM-dd)</label></td>
                <td>
                    <asp:TextBox ID="txtHireDate" runat="server" />
                </td>
            </tr>
            <tr>
                <td><label for="chkSalariedFlag">SalariedFlag</label></td>
                <td><asp:CheckBox ID="chkSalariedFlag" runat="server" /></td>
            </tr>
            <tr>
                <td><label for="txtVacationHours">VacationHours</label></td>
                <td>
                    <asp:TextBox ID="txtVacationHours" runat="server" />
                </td>
            </tr>
            <tr>
                <td><label for="txtSickLeaveHours">SickLeaveHours</label></td>
                <td>
                    <asp:TextBox ID="txtSickLeaveHours" runat="server" />
                </td>
            </tr>
            <tr>
                <td><label for="chkCurrentFlag">CurrentFlag</label></td>
                <td><asp:CheckBox ID="chkCurrentFlag" runat="server" Checked="true" /></td>
            </tr>
        </table>
        <p>
            <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" />
            <asp:Button ID="btnCancel" runat="server" Text="Cancel" CausesValidation="false" OnClick="btnCancel_Click" />
        </p>
    </form>
</body>
</html>
