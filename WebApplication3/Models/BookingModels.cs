using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WebApplication3.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public bool HasProjector { get; set; }
        public bool HasWhiteboard { get; set; }
        public bool IsActive { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BookingStatus
    {
        Planned,
        Started,
        Finished,
        Cancelled
    }

    public class Booking
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ParticipantsCount { get; set; }
        public string Topic { get; set; }
        public BookingStatus Status { get; set; }
    }

    public class BookingLog
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
    }

    public class RoomRequest
    {
        public string Name { get; set; }
        public int Capacity { get; set; }
        public bool HasProjector { get; set; }
        public bool HasWhiteboard { get; set; }
        public bool? IsActive { get; set; }
    }

    public class EmployeeRequest
    {
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
    }

    public class BookingCreateRequest
    {
        public int RoomId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ParticipantsCount { get; set; }
        public string Topic { get; set; }
    }

    public class BookingUpdateRequest
    {
        public int RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ParticipantsCount { get; set; }
        public string Topic { get; set; }
    }

    public class BookingStatusRequest
    {
        public string Status { get; set; }
    }

    public class BookingDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ParticipantsCount { get; set; }
        public string Topic { get; set; }
        public BookingStatus Status { get; set; }
    }
}