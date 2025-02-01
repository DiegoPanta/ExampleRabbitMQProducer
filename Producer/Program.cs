using System.Text;
using Newtonsoft.Json;
using Producer;
using RabbitMQ.Client;

public class PedidoProducer
{
    private readonly ConnectionFactory _factory;

    public PedidoProducer()
    {
        _factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };
    }

    public async Task EnviarPedidoAsync(Pedido pedido)
    {
        await using var connection = await _factory.CreateConnectionAsync(); // Conexão assíncrono
        await using var channel = await connection.CreateChannelAsync(); // Cria um canal assíncrono dentro da conexão com o RabbitMQ

        //Lista de Parâmetros Adicionais(arguments)
        //| Nome do Parâmetro         | Tipo    | Descrição
        //| x-message-ttl             | int(ms) | Define o tempo de vida das mensagens na fila.Expiradas são descartadas ou movidas para o DLX.
        //| x-expires                 | int(ms) | Define o tempo de vida da fila(se ninguém consumir, será deletada).
        //| x-max-length              | int     | Define o número máximo de mensagens que a fila pode armazenar.
        //| x-max-length-bytes        | int     | Define o tamanho máximo da fila em bytes.
        //| x-dead-letter-exchange    | string  | Define um exchange alternativo para mensagens expiradas ou rejeitadas.
        //| x-dead-letter-routing-key | string  | Define uma chave de roteamento para mensagens indo para o DLX.
        //| x-max-priority            | int     | Define um nível máximo de prioridade para mensagens(ex: prioridade de 0 a 10).
        //| x-queue-mode              | string  | Define o modo da fila(default ou lazy). No modo lazy, mensagens são armazenadas em disco para economizar RAM.
        //| x-single-active-consumer  | bool    | Se true, apenas um consumidor processa mensagens por vez, mesmo que haja vários conectados.

        ////Exemplo
        //var argumentos = new Dictionary<string, object>
        //{
        //    { "x-message-ttl", 60000 }, // Mensagens expiram após 60 segundos
        //    { "x-max-length", 1000 }, // Máximo de 1000 mensagens na fila
        //    { "x-dead-letter-exchange", "DLX" } // Mensagens expiradas vão para o DLX
        //};

        //// Declara a fila com os parâmetros adicionais
        //await channel.QueueDeclareAsync(queue: "fila_pedidos",
        //                                durable: true,
        //                                exclusive: false,
        //                                autoDelete: false,
        //                                arguments: argumentos);

        // Declarar a fila
        await channel.QueueDeclareAsync(queue: "fila_pedidos", // Nome da fila no RabbitMQ
                                        durable: true, // A fila será persistente (sobrevive a reinicializações do servidor)
                                        exclusive: false, // Permite que vários consumidores acessem a fila (não exclusiva a uma conexão)
                                        autoDelete: false, // A fila NÃO será deletada automaticamente quando não houver consumidores
                                        arguments: null); // Parâmetros adicionais (ex: TTL, tamanho máximo da fila) - null significa nenhum argumento extra

        var mensagem = JsonConvert.SerializeObject(pedido); // Serializa o objeto "pedido" em uma string JSON
        var body = Encoding.UTF8.GetBytes(mensagem); // Converte a string JSON para um array de bytes, para ser enviada ao RabbitMQ

        //* Tipos de exchangeType

        //| Tipo de Exchange      |  Descrição
        //| ExchangeType.Direct   |  Roteia mensagens exatamente para filas com a routing key correspondente.
        //| ExchangeType.Fanout   |  Envia a mensagem para todas as filas ligadas ao exchange, ignorando a routing key.
        //| ExchangeType.Topic    |  Roteia mensagens com base em padrões na routing key(logs.info, logs.error).
        //| ExchangeType.Headers  |  Roteia mensagens com base em valores nos headers da mensagem, ignorando a routing key.

        //PublicationAddress(string exchangeType, string exchangeName, string routingKey)
        //PublicationAddress(ExchangeType.Direct, "", "fila_pedidos")
        //PublicationAddress(ExchangeType.Direct, "meu_exchange", "minha_routing_key")
        //PublicationAddress(ExchangeType.Fanout, "broadcast_exchange", "")
        //PublicationAddress(ExchangeType.Topic, "logs_topic_exchange", "logs.error")
        ////Headers
        //var properties = new BasicProperties();
        //properties.Headers = new Dictionary<string, object> { { "tipo", "pedido" } };
        //await channel.BasicPublishAsync(
        //    new PublicationAddress(ExchangeType.Headers, "headers_exchange", ""),
        //    properties,
        //    body
        //);

        //* Parâmetros do BasicProperties
        //| Parâmetro      | Tipo                        | Descrição
        //| Persistent     | bool                        | Se true, a mensagem será salva em disco(importante para evitar perda de mensagens).
        //| Expiration     | string                      | Define o tempo de vida da mensagem(TTL) em milissegundos(ex: "60000" = 60s).
        //| Priority       | byte                        | Define a prioridade da mensagem(0 = menor, 255 = maior).
        //| Headers        | IDictionary<string, object> | Define valores personalizados, como metadados e tags.
        //| MessageId      | string                      | Define um ID único para a mensagem(ex: GUID).
        //| CorrelationId  | string                      | Relaciona mensagens de requisição e resposta(usado em RPC).
        //| ContentType    | string                      | Define o tipo do conteúdo da mensagem(ex: "application/json").
        //| ReplyTo        | string                      | Define a fila de resposta usada no padrão RPC.

        //var properties = new BasicProperties
        //{
        //    Persistent = true, // Garante que a mensagem será salva em disco (evita perda)
        //    Expiration = "60000", // Mensagem expira após 60 segundos (TTL)
        //    Priority = 5, // Define prioridade da mensagem (0 a 255)
        //    MessageId = Guid.NewGuid().ToString(), // Define um ID único para a mensagem
        //    CorrelationId = "12345", // Útil para rastrear mensagens em RPC
        //    ContentType = "application/json" // Especifica que o conteúdo é JSON
        //};

        //// Publica a mensagem no RabbitMQ com propriedades personalizadas
        //await channel.BasicPublishAsync(
        //    new PublicationAddress(ExchangeType.Direct, "", "fila_pedidos"),
        //    properties,
        //    Encoding.UTF8.GetBytes("{\"pedido\": 123}")
        //);

        await channel.BasicPublishAsync(
            new PublicationAddress(ExchangeType.Direct, "", "fila_pedidos"), //Define o destino da mensagem(Exchange e Routing Key)
            new BasicProperties(), //Propriedades opcionais da mensagem (Headers, TTL, etc.)
            body //Conteúdo da mensagem em formato de bytes
        );

        Console.WriteLine($"Pedido enviado: {mensagem}");
    }
}

// Exemplo de uso
public class Program
{
    static async Task Main()
    {
        var producer = new PedidoProducer();

        var pedido = new Pedido
        {
            Id = 1,
            Cliente = "João Silva",
            ValorTotal = 150.50
        };

        await producer.EnviarPedidoAsync(pedido);
    }
}