using System.Text;
using System.Text.Json;
using ChatRoomWithBot.Domain;
using ChatRoomWithBot.Domain.Events;
using ChatRoomWithBot.Domain.Interfaces; 
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatRoomWithBot.UI.MVC.BackgroundServices
{
    public class Worker : BackgroundService
    {

        private IConnection _connection;
        private IChannel? _channel;
        private readonly IBerechitLogger _berechitLogger;
        private readonly IServiceScopeFactory _serviceScopeFactory;


        public Worker(  IServiceScopeFactory serviceScopeFactory)
        {


            using var scope = serviceScopeFactory.CreateScope();
            var scopedServiceProvider = scope
                .ServiceProvider;
            var berechitLogger = scopedServiceProvider
                .GetRequiredService<IBerechitLogger>();


            _berechitLogger = berechitLogger;
            _serviceScopeFactory = serviceScopeFactory;

            InitializeRabbitMQ().GetAwaiter().GetResult();

        }

        private async Task  InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = SharedSettings.Current.RabbitMq.Host,
                Port = SharedSettings.Current.RabbitMq.Port,
                UserName = SharedSettings.Current.RabbitMq.Username,
                Password = SharedSettings.Current.RabbitMq.Password
            };

            _connection =await  factory.CreateConnectionAsync( );
            _channel = await _connection.CreateChannelAsync();

            _channel.QueueDeclareAsync( queue: SharedSettings.Current.RabbitMq.Queue,
              durable: true,
              exclusive: false,
              autoDelete: false,
              arguments: null);

            _berechitLogger.Information("Conectado ao RabbitMQ e fila declarada.");
        }


        protected override async  Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);


            consumer.ReceivedAsync  += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var chatMessage = JsonSerializer.Deserialize<ChatMessageCommandEvent>(message);

                using var scope = _serviceScopeFactory.CreateScope();
                var scopedServiceProvider = scope
                    .ServiceProvider;
                var processarService = scopedServiceProvider
                    .GetRequiredService<IProcessarChatMessageCommandEvent>();


                var processado =   processarService
                        .ProcessarChatMessageCommandEventAsync(chatMessage).GetAwaiter().GetResult() ;

                if (processado.Success )
                {
                    _berechitLogger.Information("Mensagem processada com sucesso => Cobrança Expirada");
                      await _channel.BasicAckAsync( deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                else
                {
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                    _berechitLogger.Error($"Mensagem não  processada com sucesso - {SharedSettings.Current.RabbitMq.Queue}"); 


                }
            };

            await _channel.BasicConsumeAsync( queue: SharedSettings.Current.RabbitMq.Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            
        }

    }
}
