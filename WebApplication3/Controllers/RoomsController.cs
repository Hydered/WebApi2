using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [RoutePrefix("api/rooms")]
    public class RoomsController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            lock (BookingStore.SyncRoot)
            {
                return Ok(BookingStore.Rooms.ToList());
            }
        }

        [HttpGet]
        [Route("{id:int}", Name = "GetRoomById")]
        public IHttpActionResult Get(int id)
        {
            lock (BookingStore.SyncRoot)
            {
                var room = BookingStore.Rooms.FirstOrDefault(item => item.Id == id);
                return room == null ? (IHttpActionResult)Content(HttpStatusCode.NotFound, new { message = "Переговорная комната не найдена." }) : Ok(room);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post(RoomRequest request)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.Name))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Название комнаты обязательно." });
            }
            if (request.Capacity <= 0)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Вместимость должна быть больше нуля." });
            }

            lock (BookingStore.SyncRoot)
            {
                if (BookingStore.Rooms.Any(existingRoom => String.Equals(existingRoom.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Комната с таким названием уже существует." });
                }

                var room = new Room
                {
                    Id = BookingStore.NextRoomId(),
                    Name = request.Name.Trim(),
                    Capacity = request.Capacity,
                    HasProjector = request.HasProjector,
                    HasWhiteboard = request.HasWhiteboard,
                    IsActive = request.IsActive ?? true
                };
                BookingStore.Rooms.Add(room);
                return CreatedAtRoute("GetRoomById", new { id = room.Id }, room);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Put(int id, RoomRequest request)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.Name))
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Название комнаты обязательно." });
            }
            if (request.Capacity <= 0)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Вместимость должна быть больше нуля." });
            }

            lock (BookingStore.SyncRoot)
            {
                var room = BookingStore.Rooms.FirstOrDefault(item => item.Id == id);
                if (room == null)
                {
                    return Content(HttpStatusCode.NotFound, new { message = "Переговорная комната не найдена." });
                }
                if (BookingStore.Rooms.Any(item => item.Id != id && String.Equals(item.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Комната с таким названием уже существует." });
                }

                room.Name = request.Name.Trim();
                room.Capacity = request.Capacity;
                room.HasProjector = request.HasProjector;
                room.HasWhiteboard = request.HasWhiteboard;
                if (request.IsActive.HasValue)
                {
                    room.IsActive = request.IsActive.Value;
                }
                return Ok(room);
            }
        }

        [HttpPut]
        [Route("{id:int}/deactivate")]
        public IHttpActionResult Deactivate(int id)
        {
            lock (BookingStore.SyncRoot)
            {
                var room = BookingStore.Rooms.FirstOrDefault(item => item.Id == id);
                if (room == null)
                {
                    return Content(HttpStatusCode.NotFound, new { message = "Переговорная комната не найдена." });
                }
                if (!room.IsActive)
                {
                    return Content(HttpStatusCode.Conflict, new { message = "Комната уже деактивирована." });
                }
                room.IsActive = false;
                return StatusCode(HttpStatusCode.NoContent);
            }
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available(DateTime? start = null, DateTime? end = null, int? participants = null,
            bool? projector = null, bool? whiteboard = null)
        {
            if (!start.HasValue || !end.HasValue || start.Value >= end.Value)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Укажите корректный интервал: start должен быть раньше end." });
            }
            if (participants.HasValue && participants.Value <= 0)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Количество участников должно быть больше нуля." });
            }

            lock (BookingStore.SyncRoot)
            {
                var rooms = BookingStore.Rooms.Where(room =>
                    room.IsActive &&
                    (!participants.HasValue || room.Capacity >= participants.Value) &&
                    (!(projector ?? false) || room.HasProjector) &&
                    (!(whiteboard ?? false) || room.HasWhiteboard) &&
                    !BookingStore.HasTimeConflict(room.Id, start.Value, end.Value, null)).ToList();
                return Ok(rooms);
            }
        }
    }
}