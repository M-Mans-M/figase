using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Figase.Hubs
{
    [Authorize]
    public class NewsHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public readonly static Dictionary<int, string> Connections = new Dictionary<int, string>();

        /// <summary>
        /// Событие об успешном соединении клиента
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            var userIdRaw = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userId = Int32.Parse(userIdRaw);

            if (userId == 0)
            {
                // Разорвать соединение с неопознанным объектом (хотя он в принципе не должен был пройти авторизацию по сертификату)
                Debug.WriteLine($"WARN: Connected unauthorized client! Abort connection!");
                Context.Abort();
                return;
            }

            Debug.WriteLine($"TRACE: User {userId} ({Context.ConnectionId}) connected.");

            Connections[userId] = Context.ConnectionId;
            //await Groups.AddToGroupAsync(Context.ConnectionId, $"terminal.{ClientName}");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Событие об отключении клиента
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userIdRaw = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userId = Int32.Parse(userIdRaw);

            if (userId == 0)
            {
                // Разорвать соединение с неопознанным объектом (хотя он в принципе не должен был пройти авторизацию по сертификату)
                Debug.WriteLine($"WARN: Disconnected unauthorized client! Abort connection!");
                Context.Abort();
                return;
            }

            Debug.WriteLine($"TRACE: User {userId} ({Context.ConnectionId}) disconnected.");

            Connections.Remove(userId);
            //await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"terminal.{ClientName}");

            await base.OnDisconnectedAsync(exception);
        }
    }
}
