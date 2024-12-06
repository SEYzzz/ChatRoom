using Microsoft.AspNetCore.SignalR;
using SignalRChatRoom.Server.InMemoryData;
using SignalRChatRoom.Server.Models;

namespace SignalRChatRoom.Server.Hubs
{
    public class ChatHub : Hub
    {
        public async Task GetUsernameAsync(string username)
        {
            Client client = new Client { ConnectionId = Context.ConnectionId, Username = username };

            ClientSource.Clients.Add(client);

            await Clients.Others.SendAsync("clientJoined", username);

            await GetClientsAsync();

            await Clients.Caller.SendAsync("groups", GroupSource.Groups);
        }

        public async Task GetClientsAsync()
        {
            await Clients.All.SendAsync("clients", ClientSource.Clients);
        }

        public async Task SendMessageAsync(string message, Client client)
        {
            Client senderClient = ClientSource.Clients.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId);

            await Clients.Client(client.ConnectionId).SendAsync("receiveMessage", message, senderClient, client, null);
        }

        public async Task AddGroupAsync(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            Group group = new Group { GroupName = groupName };

            group.Clients.Add(ClientSource.Clients.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId));

            GroupSource.Groups.Add(group);

            await Clients.All.SendAsync("groupAdded", groupName);

            await GetGroupsAsync();
        }

        public async Task GetGroupsAsync()
        {
            await Clients.All.SendAsync("groups", GroupSource.Groups);
        }

        public async Task AddClientToGroupsAsync(IEnumerable<string> groupNames)
        {
            Client client = ClientSource.Clients.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId);

            foreach (var groupName in groupNames)
            {
                Group _group = GroupSource.Groups.FirstOrDefault(g => g.GroupName == groupName);

                var result = _group.Clients.Any(c => c.ConnectionId == Context.ConnectionId);
                if (!result)
                {
                    _group.Clients.Add(client);

                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                    await GetClientsOfGroupAsync(groupName);
                }
            }
        }

        public async Task AddClientToGroupAsync(string groupName)
        {
            Client client = ClientSource.Clients.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId);

            Group _group = GroupSource.Groups.FirstOrDefault(g => g.GroupName == groupName);

            var result = _group.Clients.Any(c=> c.ConnectionId == Context.ConnectionId);
            if (!result)
            {
                _group.Clients.Add(client);

                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                await GetClientsOfGroupAsync(groupName);
            }
            
        }

        public async Task GetClientsOfGroupAsync(string groupName)
        {
            Group group = GroupSource.Groups.FirstOrDefault(g => g.GroupName.Equals(groupName));

            await Clients.Groups(groupName).SendAsync("clientsOfGroup", group.Clients, group.GroupName);
        }

        public async Task SendMessageToGroupAsync(string groupName, string message)
        {
            await Clients.Groups(groupName).SendAsync("receiveMessage", message, ClientSource.Clients.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId), null, groupName);
        }

        //public override async Task OnConnectedAsync()
        //{
        //    await Clients.All.SendAsync("clientJoined", Context.ConnectionId);
        //}

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Client client = ClientSource.Clients.First(c => c.ConnectionId == Context.ConnectionId);
            await Clients.All.SendAsync("clientLeaved", client != null ? client.Username : null);

            ClientSource.Clients.Remove(client);

            await GetClientsAsync();

            foreach (var group in GroupSource.Groups)
            {
                var result = group.Clients.Any(c => c.ConnectionId == Context.ConnectionId);
                if (result)
                {
                    group.Clients.Remove(client);

                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, group.GroupName);

                    await GetClientsOfGroupAsync(group.GroupName);
                }
            }
        }
    }
}
