using ChatRoomWithBot.Domain;
using ChatRoomWithBot.Domain.Bus;
using ChatRoomWithBot.Domain.Events;
using ChatRoomWithBot.Domain.Interfaces;
using MediatR;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace ChatRoomWithBot.Services.RabbitMq.Handler
{
    internal class BotMessageNotificationHandler : IRequestHandler<ChatMessageCommandEvent, CommandResponse>
    {
        private readonly IBerechitLogger _berechitLogger;

        public BotMessageNotificationHandler(IBerechitLogger berechitLogger)
        {
            _berechitLogger = berechitLogger;
        }

        public async Task<CommandResponse> Handle(ChatMessageCommandEvent notification,
            CancellationToken cancellationToken)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = SharedSettings.Current.RabbitMq.Host,
                    Port = SharedSettings.Current.RabbitMq.Port,
                    UserName = SharedSettings.Current.RabbitMq.Username,
                    Password = SharedSettings.Current.RabbitMq.Password
                };

             await    using (var connection = await factory.CreateConnectionAsync(cancellationToken))


            await     using (var channel =   await connection.CreateChannelAsync(cancellationToken: cancellationToken))
                {
                    // Declaração da fila
                    await channel.QueueDeclareAsync(
                      queue: SharedSettings.Current.RabbitMq.Queue,
                      durable: true,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);

                    // Serialização da mensagem
                    var message = JsonConvert.SerializeObject(notification);
                    var body = Encoding.UTF8.GetBytes(message);



                    // Publicação da mensagem
                  await   channel.BasicPublishAsync( 
                       exchange: "",
                       routingKey: "ChatMessageCommandEvent",
                       body: body, cancellationToken: cancellationToken);
                }


                return CommandResponse.Ok();
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                _berechitLogger.Error(ex);
                return CommandResponse.Fail(ex);
            }
        }
    }
}
