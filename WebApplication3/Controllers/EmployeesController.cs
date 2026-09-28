using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [RoutePrefix("api/employees")]
    public class EmployeesController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            lock (BookingStore.SyncRoot)
            {
                return Ok(BookingStore.Employees.ToList());
            }
        }

        [HttpGet]
        [Route("{id:int}", Name = "GetEmployeeById")]
        public IHttpActionResult Get(int id)
        {
            lock (BookingStore.SyncRoot)
            {
                var employee = BookingStore.Employees.FirstOrDefault(item => item.Id == id);
                return employee == null ? (IHttpActionResult)Content(HttpStatusCode.NotFound, new { message = "Сотрудник не найден." }) : Ok(employee);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post(EmployeeRequest request)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.FullName))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "ФИО сотрудника обязательно." });
            }
            if (String.IsNullOrWhiteSpace(request.Email))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Email сотрудника обязателен." });
            }

            lock (BookingStore.SyncRoot)
            {
                if (BookingStore.Employees.Any(existingEmployee => String.Equals(existingEmployee.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Сотрудник с таким Email уже существует." });
                }
                var employee = new Employee
                {
                    Id = BookingStore.NextEmployeeId(),
                    FullName = request.FullName.Trim(),
                    Department = request.Department == null ? null : request.Department.Trim(),
                    Email = request.Email.Trim()
                };
                BookingStore.Employees.Add(employee);
                return CreatedAtRoute("GetEmployeeById", new { id = employee.Id }, employee);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Put(int id, EmployeeRequest request)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.FullName))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "ФИО сотрудника обязательно." });
            }
            if (String.IsNullOrWhiteSpace(request.Email))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Email сотрудника обязателен." });
            }

            lock (BookingStore.SyncRoot)
            {
                var employee = BookingStore.Employees.FirstOrDefault(item => item.Id == id);
                if (employee == null)
                {
                    return Content(HttpStatusCode.NotFound, new { message = "Сотрудник не найден." });
                }
                if (BookingStore.Employees.Any(item => item.Id != id && String.Equals(item.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Сотрудник с таким Email уже существует." });
                }

                employee.FullName = request.FullName.Trim();
                employee.Department = request.Department == null ? null : request.Department.Trim();
                employee.Email = request.Email.Trim();
                return Ok(employee);
            }
        }
    }
}