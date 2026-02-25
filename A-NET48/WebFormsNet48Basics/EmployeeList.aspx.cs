using System;
using System.Linq;
using WebFormsNet48Basics.Data;

namespace WebFormsNet48Basics
{
    public partial class EmployeeList : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var message = Request.QueryString["message"];
                if (!string.IsNullOrWhiteSpace(message))
                {
                    lblMessage.Text = message;
                }

                BindEmployees();
            }
        }

        protected void gvEmployees_RowDeleting(object sender, System.Web.UI.WebControls.GridViewDeleteEventArgs e)
        {
            var employeeId = (int)gvEmployees.DataKeys[e.RowIndex].Value;

            using (var context = new AdventureWorksContext())
            {
                var employee = context.Employees.FirstOrDefault(x => x.BusinessEntityID == employeeId);
                if (employee != null)
                {
                    context.Employees.Remove(employee);
                    context.SaveChanges();
                    lblMessage.Text = "Employee deleted.";
                }
            }

            BindEmployees();
        }

        private void BindEmployees()
        {
            using (var context = new AdventureWorksContext())
            {
                var employees = context.Employees
                    .OrderBy(x => x.BusinessEntityID)
                    .Take(200)
                    .ToList();

                gvEmployees.DataSource = employees;
                gvEmployees.DataBind();
            }
        }
    }
}

