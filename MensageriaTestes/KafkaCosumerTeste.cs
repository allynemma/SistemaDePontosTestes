using Confluent.Kafka;
using SistemaDePontosAPI.Mensageria;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class KafkaConsumerTeste
{
    private readonly string _bootstrapServers = "localhost:9092";
    private readonly string _topic = "punch-clock";

    [Fact]
    public async Task ShouldReceiveMessageFromKafka()
    {
        // Arrange
        var config = new ConsumerConfig
        {
            GroupId = "test-consumer-group",
            BootstrapServers = _bootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var producer = new KafkaProducer(_bootstrapServers, _topic);

        // Produzindo a mensagem para o Kafka
        await producer.SendMessageAsync("Usuário 1 registrou um CheckIn");

        var messages = new List<string>();

        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            consumer.Subscribe(_topic);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(10)); // Timeout aumentado para 10 segundos

            // Act
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cts.Token);
                    if (consumeResult != null)
                    {
                        messages.Add(consumeResult.Message.Value);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignorar exceção de cancelamento
            }
            catch (Exception ex)
            {
                // Logar outras exceções
                throw new Exception("Erro ao consumir mensagem do Kafka", ex);
            }
            finally
            {
                consumer.Close();
            }
        }

        // Assert
        Assert.Single(messages);
        Assert.Equal("Usuário 1 registrou um CheckIn", messages[0]);
    }
}
