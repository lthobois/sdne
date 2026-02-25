using System;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using WebFormsNet48Basics.Data;
using WebFormsNet48Basics.Models;

namespace WebFormsNet48Basics
{
    public partial class EmployeeForm : System.Web.UI.Page
    {
        private int? EmployeeId
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
                if (EmployeeId.HasValue)
                {
                    litTitle.Text = "Edit Employee";
                    txtBusinessEntityID.Enabled = false;
                    LoadEmployee(EmployeeId.Value);
                }
                else
                {
                    litTitle.Text = "Create Employee";
                    chkSalariedFlag.Checked = true;
                    chkCurrentFlag.Checked = true;
                    txtBirthDate.Text = DateTime.Today.AddYears(-25).ToString("yyyy-MM-dd");
                    txtHireDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                }
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            try
            {
                using (var context = new AdventureWorksContext())
                {
                    Employee employee;

                    if (EmployeeId.HasValue)
                    {
                        employee = context.Employees.FirstOrDefault(x => x.BusinessEntityID == EmployeeId.Value);
                        if (employee == null)
                        {
                            lblError.Text = "Employee not found.";
                            return;
                        }
                    }
                    else
                    {
                        employee = new Employee
                        {
                            BusinessEntityID = ParseInt(txtBusinessEntityID.Text),
                            rowguid = Guid.NewGuid()
                        };
                        context.Employees.Add(employee);
                    }

                    employee.NationalIDNumber = txtNationalIDNumber.Text;
                    employee.LoginID = txtLoginID.Text;
                    employee.JobTitle = txtJobTitle.Text;
                    employee.BirthDate = ParseDate(txtBirthDate.Text);
                    employee.MaritalStatus = ddlMaritalStatus.SelectedValue;
                    employee.Gender = ddlGender.SelectedValue;
                    employee.HireDate = ParseDate(txtHireDate.Text);
                    employee.SalariedFlag = chkSalariedFlag.Checked;
                    employee.VacationHours = ParseShort(txtVacationHours.Text);
                    employee.SickLeaveHours = ParseShort(txtSickLeaveHours.Text);
                    employee.CurrentFlag = chkCurrentFlag.Checked;
                    employee.ModifiedDate = DateTime.Now;

                    context.SaveChanges();
                }

                Response.Redirect("~/EmployeeList.aspx?message=Employee saved", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (DbUpdateException)
            {
                lblError.Text = "Save failed. Verify SQL constraints and BusinessEntityID relationship with Person table.";
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/EmployeeList.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void LoadEmployee(int employeeId)
        {
            using (var context = new AdventureWorksContext())
            {
                var employee = context.Employees.FirstOrDefault(x => x.BusinessEntityID == employeeId);
                if (employee == null)
                {
                    lblError.Text = "Employee not found.";
                    btnSave.Enabled = false;
                    return;
                }

                txtBusinessEntityID.Text = employee.BusinessEntityID.ToString(CultureInfo.InvariantCulture);
                txtNationalIDNumber.Text = employee.NationalIDNumber;
                txtLoginID.Text = employee.LoginID;
                txtJobTitle.Text = employee.JobTitle;
                txtBirthDate.Text = employee.BirthDate.ToString("yyyy-MM-dd");
                ddlMaritalStatus.SelectedValue = employee.MaritalStatus.Trim().ToUpperInvariant();
                ddlGender.SelectedValue = employee.Gender.Trim().ToUpperInvariant();
                txtHireDate.Text = employee.HireDate.ToString("yyyy-MM-dd");
                chkSalariedFlag.Checked = employee.SalariedFlag;
                txtVacationHours.Text = employee.VacationHours.ToString(CultureInfo.InvariantCulture);
                txtSickLeaveHours.Text = employee.SickLeaveHours.ToString(CultureInfo.InvariantCulture);
                chkCurrentFlag.Checked = employee.CurrentFlag;
            }
        }

        private static int ParseInt(string value)
        {
            int result;
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            return result;
        }

        private static short ParseShort(string value)
        {
            short result;
            short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            return result;
        }

        private static DateTime ParseDate(string value)
        {
            DateTime result;
            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return DateTime.Now;
            }

            return result;
        }
    }
}
