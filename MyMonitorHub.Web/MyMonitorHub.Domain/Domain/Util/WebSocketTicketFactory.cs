using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Util
{
// ReSharper disable once ClassNeverInstantiated.Global
    public class WebSocketTicketFactory
    {
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, WebSocketTicket> _tickets = new Dictionary<string, WebSocketTicket>();

        private const int TicketTtlSeconds = 30;

        public WebSocketTicket Create()
        {
            var ticket = new WebSocketTicket
            {
                Username = Helper.Email,
                AccountId = Helper.AccountId,
                Ticket = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
            lock (_lockObject)
            {
                PurgeExpired();
                _tickets[ticket.Ticket] = ticket;
            }
            return ticket;
        }

        public WebSocketTicket Validate(string ticket)
        {
            lock (_lockObject)
            {
                if (!_tickets.TryGetValue(ticket, out var ticketObject))
                    return null;

                _tickets.Remove(ticket);

                if ((DateTime.UtcNow - ticketObject.CreatedAt).TotalSeconds > TicketTtlSeconds)
                    return null;

                return ticketObject;
            }
        }

        private void PurgeExpired()
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-TicketTtlSeconds);
            var expired = new List<string>();
            foreach (var kvp in _tickets)
            {
                if (kvp.Value.CreatedAt < cutoff)
                    expired.Add(kvp.Key);
            }
            foreach (var key in expired)
                _tickets.Remove(key);
        }
    }

    public class WebSocketTicket
    {
        public string Username { get; set; }
        public int AccountId { get; set; }
        public string Ticket { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
