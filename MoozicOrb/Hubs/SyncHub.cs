using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MoozicOrb.Hubs
{
    public class SyncHub : Hub
    {
        public async Task JoinStation()
        {
           await Clients.All.SendAsync("Response", MoozicOrb.Station.JoinStation("Station 1"));
        }

        public async Task SendMessage(int receiver_id, string message_text)
        {
            long message_id = new MoozicOrb.IO.InsertDirectMessage().Insert(1, 2, "another message");

            string conId = Context.ConnectionId.ToString();
            await Clients.Client(Context.ConnectionId).SendAsync("Message", "Did you get the message + " + conId);
           
        }

        public async Task SendGroupMessage(int group_id, int receiver_id, string message_text)
        {
            //send message to db before the receipient. please correct the logic..
            //needs user_id and receiving_user_id {10, receiver_id}
            //group_id needs some type of convention. group_id -> group identifier.
            
            //int message_id = new MoozicOrb.IO.CreateMessage().InsertMessage(group_id, 10, receiver_id, message_text);
            
            // change int db group_id to signal r group id
            string _group_id = group_id.ToString();

            string conId = Context.ConnectionId.ToString();
            //await Clients.Client(receiver_id + "").SendAsync("Message", message_text); // correct .Client w/ receiver_id
            await Clients.Group(_group_id).SendAsync("GroupMessage", message_text + " " +
                group_id + " " + receiver_id);



        }
    }
}
