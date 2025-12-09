using System;
using System.Collections.Generic;

namespace InfoPanel.VBZ.Models
{
    public class Departure
    {
        public string Line { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTime DepartureTime { get; set; }
        public bool IsRealtime { get; set; }
        public bool IsAccessible { get; set; }
        public bool IsLate { get; set; }

        // New properties
        public string Platform { get; set; } = "";
        public string TransportMode { get; set; } = ""; // e.g. tram, bus
        public string Operator { get; set; } = "";
        public string LineBackgroundColor { get; set; } = ""; // Hex code
        public string LineTextColor { get; set; } = ""; // Hex code

        public string FormattedTime
        {
            get
            {
                var diff = DepartureTime - DateTime.Now;
                if (diff.TotalMinutes <= 0) return "0'";
                if (diff.TotalMinutes < 60) return $"{Math.Ceiling(diff.TotalMinutes)}'";
                return DepartureTime.ToString("HH:mm");
            }
        }
    }

    /// <summary>
    /// Data model for VBZ transport information
    /// </summary>
    public class VbzData
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }

        // New property
        public string StationName { get; set; } = "";

        public List<Departure> Departures { get; set; } = new();
    }
}
