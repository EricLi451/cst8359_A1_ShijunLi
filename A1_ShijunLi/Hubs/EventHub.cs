using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace A1_ShijunLi.Hubs
{
    // Hub 是 SignalR 的核心，就像是一个无线电发射塔
    public class EventHub : Hub
    {
        // 当用户打开某个 Event 的详情页时，把他加入这个频道的“群组”
        public async Task JoinEventGroup(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
        }

        // 当用户离开详情页时，把他移出群组
        public async Task LeaveEventGroup(string eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
        }
    }
}