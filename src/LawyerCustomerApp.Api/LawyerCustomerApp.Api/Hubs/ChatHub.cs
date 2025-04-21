//using LawyerCustomerApp.Domain.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.SignalR;
//using System.Security.Claims;

//namespace LawyerCustomerApp.Application.Hubs;

//public class ChatHub : Hub
//{
//    private readonly IChatService _chatService;

//    public ChatHub(IChatService chatService)
//    {
//        _chatService = chatService;
//    }

//    public async Task SendMessage(int receiverId, string content)
//    {
//        if (int.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var senderId))
//            senderId = 0;

//        await _chatService.SendMessageAsync(senderId, receiverId, content);
        
//        await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", senderId, content);
//    }
//}