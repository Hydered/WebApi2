using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Models
{
    public static class BookingStore
    {
        public static readonly object SyncRoot = new object();
        public static readonly List<Room> Rooms = new List<Room>();
        public static readonly List<Employee> Employees = new List<Employee>();
        public static readonly List<Booking> Bookings = new List<Booking>();
        public static readonly List<BookingLog> Logs = new List<BookingLog>();

        static BookingStore()
        {
            Rooms.AddRange(new[]
            {
                new Room { Id = 1, Name = "Orion", Capacity = 4, HasProjector = false, HasWhiteboard = true, IsActive = true },
                new Room { Id = 2, Name = "Atlas", Capacity = 8, HasProjector = true, HasWhiteboard = true, IsActive = true },
                new Room { Id = 3, Name = "Neva", Capacity = 6, HasProjector = true, HasWhiteboard = false, IsActive = true },
                new Room { Id = 4, Name = "Baikal", Capacity = 12, HasProjector = true, HasWhiteboard = true, IsActive = true },
                new Room { Id = 5, Name = "Ladoga", Capacity = 3, HasProjector = false, HasWhiteboard = false, IsActive = true },
                new Room { Id = 6, Name = "Archive", Capacity = 10, HasProjector = true, HasWhiteboard = false, IsActive = false }
            });

            Employees.AddRange(new[]
            {
                new Employee { Id = 1, FullName = "Анна Смирнова", Department = "Разработка", Email = "anna.smirnova@example.com" },
                new Employee { Id = 2, FullName = "Иван Петров", Department = "Финансы", Email = "ivan.petrov@example.com" },
                new Employee { Id = 3, FullName = "Мария Иванова", Department = "Маркетинг", Email = "maria.ivanova@example.com" },
                new Employee { Id = 4, FullName = "Алексей Соколов", Department = "Продажи", Email = "alexey.sokolov@example.com" },
                new Employee { Id = 5, FullName = "Ольга Кузнецова", Department = "Кадры", Email = "olga.kuznetsova@example.com" },
                new Employee { Id = 6, FullName = "Дмитрий Волков", Department = "Поддержка", Email = "dmitry.volkov@example.com" },
                new Employee { Id = 7, FullName = "Елена Морозова", Department = "Юридический отдел", Email = "elena.morozova@example.com" },
                new Employee { Id = 8, FullName = "Сергей Новиков", Department = "Аналитика", Email = "sergey.novikov@example.com" }
            });

            var today = DateTime.Today;
            Bookings.AddRange(new[]
            {
                SeedBooking(1, 1, 1, today.AddDays(1).AddHours(9), today.AddDays(1).AddHours(10), 4, "Планирование спринта", BookingStatus.Planned),
                SeedBooking(2, 1, 2, today.AddDays(1).AddHours(10), today.AddDays(1).AddHours(11), 3, "Обзор проекта", BookingStatus.Started),
                SeedBooking(3, 2, 3, today.AddDays(-1).AddHours(9), today.AddDays(-1).AddHours(10), 6, "Итоги кампании", BookingStatus.Finished),
                SeedBooking(4, 3, 4, today.AddDays(2).AddHours(13), today.AddDays(2).AddHours(14), 5, "Встреча с клиентом", BookingStatus.Cancelled),
                SeedBooking(5, 4, 5, today.AddDays(3).AddHours(10), today.AddDays(3).AddHours(11), 10, "Квартальный обзор", BookingStatus.Planned),
                SeedBooking(6, 5, 6, today.AddDays(4).AddHours(11), today.AddDays(4).AddHours(12), 3, "Синхронизация поддержки", BookingStatus.Started),
                SeedBooking(7, 6, 7, today.AddDays(-2).AddHours(14), today.AddDays(-2).AddHours(15), 8, "Обсуждение договора", BookingStatus.Finished),
                SeedBooking(8, 1, 8, today.AddDays(5).AddHours(9), today.AddDays(5).AddHours(10), 2, "Проверка метрик", BookingStatus.Planned),
                SeedBooking(9, 2, 1, today.AddDays(6).AddHours(14), today.AddDays(6).AddHours(15), 7, "Обзор аналитики", BookingStatus.Started),
                SeedBooking(10, 3, 2, today.AddDays(7).AddHours(10), today.AddDays(7).AddHours(11), 4, "Планирование бюджета", BookingStatus.Planned)
            });

            foreach (var booking in Bookings)
            {
                AddLog(booking.Id, "Created", "Бронирование добавлено в начальные данные.");
            }
        }

        public static int NextRoomId()
        {
            return Rooms.Count == 0 ? 1 : Rooms.Max(room => room.Id) + 1;
        }

        public static int NextEmployeeId()
        {
            return Employees.Count == 0 ? 1 : Employees.Max(employee => employee.Id) + 1;
        }

        public static int NextBookingId()
        {
            return Bookings.Count == 0 ? 1 : Bookings.Max(booking => booking.Id) + 1;
        }

        public static void AddLog(int bookingId, string action, string details)
        {
            Logs.Add(new BookingLog
            {
                Id = Logs.Count == 0 ? 1 : Logs.Max(log => log.Id) + 1,
                BookingId = bookingId,
                CreatedAt = DateTime.Now,
                Action = action,
                Details = details
            });
        }

        public static BookingDto ToDto(Booking booking)
        {
            var room = Rooms.FirstOrDefault(item => item.Id == booking.RoomId);
            var employee = Employees.FirstOrDefault(item => item.Id == booking.EmployeeId);
            return new BookingDto
            {
                Id = booking.Id,
                RoomId = booking.RoomId,
                RoomName = room == null ? null : room.Name,
                EmployeeId = booking.EmployeeId,
                EmployeeFullName = employee == null ? null : employee.FullName,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                ParticipantsCount = booking.ParticipantsCount,
                Topic = booking.Topic,
                Status = booking.Status
            };
        }

        public static bool HasTimeConflict(int roomId, DateTime start, DateTime end, int? exceptBookingId)
        {
            return Bookings.Any(booking =>
                booking.RoomId == roomId &&
                (!exceptBookingId.HasValue || booking.Id != exceptBookingId.Value) &&
                (booking.Status == BookingStatus.Planned || booking.Status == BookingStatus.Started) &&
                start < booking.EndTime && booking.StartTime < end);
        }

        private static Booking SeedBooking(int id, int roomId, int employeeId, DateTime start, DateTime end,
            int participants, string topic, BookingStatus status)
        {
            return new Booking
            {
                Id = id,
                RoomId = roomId,
                EmployeeId = employeeId,
                StartTime = start,
                EndTime = end,
                ParticipantsCount = participants,
                Topic = topic,
                Status = status
            };
        }
    }
}