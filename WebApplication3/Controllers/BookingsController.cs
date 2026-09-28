using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [RoutePrefix("api/bookings")]
    public class BookingsController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get(int? employeeId = null, int? roomId = null, DateTime? date = null, string status = null)
        {
            BookingStatus parsedStatus;
            if (!String.IsNullOrWhiteSpace(status) && !TryParseStatus(status, out parsedStatus))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Неизвестный статус бронирования." });
            }

            lock (BookingStore.SyncRoot)
            {
                var query = BookingStore.Bookings.AsEnumerable();
                if (employeeId.HasValue) query = query.Where(booking => booking.EmployeeId == employeeId.Value);
                if (roomId.HasValue) query = query.Where(booking => booking.RoomId == roomId.Value);
                if (date.HasValue) query = query.Where(booking => booking.StartTime.Date == date.Value.Date);
                if (!String.IsNullOrWhiteSpace(status))
                {
                    TryParseStatus(status, out parsedStatus);
                    query = query.Where(booking => booking.Status == parsedStatus);
                }
                return Ok(query.OrderBy(booking => booking.StartTime).Select(BookingStore.ToDto).ToList());
            }
        }

        [HttpGet]
        [Route("{id:int}", Name = "GetBookingById")]
        public IHttpActionResult Get(int id)
        {
            lock (BookingStore.SyncRoot)
            {
                var booking = BookingStore.Bookings.FirstOrDefault(item => item.Id == id);
                return booking == null ? (IHttpActionResult)Content(HttpStatusCode.NotFound, new { message = "Бронирование не найдено." }) : Ok(BookingStore.ToDto(booking));
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post(BookingCreateRequest request)
        {
            var validation = ValidateBooking(request == null ? default(DateTime) : request.StartTime,
                request == null ? default(DateTime) : request.EndTime,
                request == null ? 0 : request.ParticipantsCount,
                request == null ? null : request.Topic);
            if (validation != null) return Content(HttpStatusCode.BadRequest, new { message = validation });

            lock (BookingStore.SyncRoot)
            {
                var room = BookingStore.Rooms.FirstOrDefault(item => item.Id == request.RoomId);
                if (room == null) return Content(HttpStatusCode.NotFound, new { message = "Переговорная комната не найдена." });
                if (!room.IsActive) return Content(HttpStatusCode.Conflict, new { message = "Комната неактивна и недоступна для бронирования." });
                var employee = BookingStore.Employees.FirstOrDefault(item => item.Id == request.EmployeeId);
                if (employee == null) return Content(HttpStatusCode.NotFound, new { message = "Сотрудник не найден." });
                if (request.ParticipantsCount > room.Capacity) return Content(HttpStatusCode.Conflict, new { message = "Вместимость комнаты недостаточна для указанного числа участников." });
                if (BookingStore.HasTimeConflict(room.Id, request.StartTime, request.EndTime, null))
                {
                    return Content(HttpStatusCode.Conflict, new { message = "На выбранное время комната уже забронирована." });
                }

                var booking = new Booking
                {
                    Id = BookingStore.NextBookingId(),
                    RoomId = room.Id,
                    EmployeeId = employee.Id,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    ParticipantsCount = request.ParticipantsCount,
                    Topic = request.Topic.Trim(),
                    Status = BookingStatus.Planned
                };
                BookingStore.Bookings.Add(booking);
                BookingStore.AddLog(booking.Id, "Created", "Бронирование создано.");
                return CreatedAtRoute("GetBookingById", new { id = booking.Id }, BookingStore.ToDto(booking));
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Put(int id, BookingUpdateRequest request)
        {
            var validation = ValidateBooking(request == null ? default(DateTime) : request.StartTime,
                request == null ? default(DateTime) : request.EndTime,
                request == null ? 0 : request.ParticipantsCount,
                request == null ? null : request.Topic);
            if (validation != null) return Content(HttpStatusCode.BadRequest, new { message = validation });

            lock (BookingStore.SyncRoot)
            {
                var booking = BookingStore.Bookings.FirstOrDefault(item => item.Id == id);
                if (booking == null) return Content(HttpStatusCode.NotFound, new { message = "Бронирование не найдено." });
                if (booking.Status == BookingStatus.Finished || booking.Status == BookingStatus.Cancelled)
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Завершенное или отмененное бронирование нельзя изменить." });
                }
                var room = BookingStore.Rooms.FirstOrDefault(item => item.Id == request.RoomId);
                if (room == null) return Content(HttpStatusCode.NotFound, new { message = "Переговорная комната не найдена." });
                if (!room.IsActive) return Content(HttpStatusCode.Conflict, new { message = "Комната неактивна и недоступна для бронирования." });
                if (room.Capacity < request.ParticipantsCount) return Content(HttpStatusCode.Conflict, new { message = "Вместимость комнаты недостаточна для указанного числа участников." });
                if (BookingStore.HasTimeConflict(room.Id, request.StartTime, request.EndTime, booking.Id))
                {
                    return Content(HttpStatusCode.Conflict, new { message = "На выбранное время комната уже забронирована." });
                }

                var details = String.Format("Комната {0}, интервал {1:O} - {2:O}, участников {3}, тема: {4}.",
                    room.Name, request.StartTime, request.EndTime, request.ParticipantsCount, request.Topic.Trim());
                booking.RoomId = room.Id;
                booking.StartTime = request.StartTime;
                booking.EndTime = request.EndTime;
                booking.ParticipantsCount = request.ParticipantsCount;
                booking.Topic = request.Topic.Trim();
                BookingStore.AddLog(booking.Id, "Updated", details);
                return Ok(BookingStore.ToDto(booking));
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            lock (BookingStore.SyncRoot)
            {
                var booking = BookingStore.Bookings.FirstOrDefault(item => item.Id == id);
                if (booking == null) return Content(HttpStatusCode.NotFound, new { message = "Бронирование не найдено." });
                if (booking.Status == BookingStatus.Started || booking.Status == BookingStatus.Finished)
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Начатое или завершенное бронирование нельзя удалить." });
                }
                BookingStore.AddLog(booking.Id, "Deleted", "Бронирование удалено.");
                BookingStore.Bookings.Remove(booking);
                return StatusCode(HttpStatusCode.NoContent);
            }
        }

        [HttpPut]
        [Route("{id:int}/status")]
        public IHttpActionResult UpdateStatus(int id, BookingStatusRequest request)
        {
            BookingStatus nextStatus;
            if (request == null || !TryParseStatus(request.Status, out nextStatus))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Укажите один из статусов: Planned, Started, Finished, Cancelled." });
            }

            lock (BookingStore.SyncRoot)
            {
                var booking = BookingStore.Bookings.FirstOrDefault(item => item.Id == id);
                if (booking == null) return Content(HttpStatusCode.NotFound, new { message = "Бронирование не найдено." });
                var allowed = (booking.Status == BookingStatus.Planned && (nextStatus == BookingStatus.Started || nextStatus == BookingStatus.Cancelled)) ||
                    (booking.Status == BookingStatus.Started && nextStatus == BookingStatus.Finished);
                if (!allowed)
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Переход из текущего статуса в указанный запрещен." });
                }

                var oldStatus = booking.Status;
                booking.Status = nextStatus;
                BookingStore.AddLog(booking.Id, "StatusChanged", String.Format("Статус изменен: {0} -> {1}.", oldStatus, nextStatus));
                if (nextStatus == BookingStatus.Cancelled)
                {
                    BookingStore.AddLog(booking.Id, "Cancelled", "Бронирование отменено.");
                }
                return Ok(BookingStore.ToDto(booking));
            }
        }

        [HttpGet]
        [Route("{id:int}/history")]
        public IHttpActionResult History(int id)
        {
            lock (BookingStore.SyncRoot)
            {
                var history = BookingStore.Logs.Where(log => log.BookingId == id).OrderBy(log => log.CreatedAt).ThenBy(log => log.Id).ToList();
                if (!BookingStore.Bookings.Any(item => item.Id == id) && history.Count == 0)
                {
                    return Content(HttpStatusCode.NotFound, new { message = "Бронирование и его история не найдены." });
                }
                return Ok(history);
            }
        }

        private static string ValidateBooking(DateTime start, DateTime end, int participants, string topic)
        {
            if (start == default(DateTime) || end == default(DateTime)) return "Укажите время начала и окончания.";
            if (start >= end) return "StartTime должен быть раньше EndTime.";
            if (start < DateTime.Now) return "StartTime не может находиться в прошлом.";
            if (participants <= 0) return "Количество участников должно быть больше нуля.";
            if (String.IsNullOrWhiteSpace(topic)) return "Тема встречи обязательна.";
            return null;
        }

        private static bool TryParseStatus(string value, out BookingStatus status)
        {
            return Enum.TryParse(value, true, out status) && Enum.IsDefined(typeof(BookingStatus), status);
        }
    }
}