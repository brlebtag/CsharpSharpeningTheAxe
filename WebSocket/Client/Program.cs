using System.Net.WebSockets;
using System.Text;

const string ServerUrl = "ws://localhost:5113/ws";

using var client = new ClientWebSocket();

Console.WriteLine($"Conectando ao servidor: {ServerUrl}");
await client.ConnectAsync(new Uri(ServerUrl), CancellationToken.None);
Console.WriteLine("Conectado.");

// Primeira mensagem do servidor contém o UID atribuído a esta conexão
var uidBuffer = new byte[64];
var uidResult = await client.ReceiveAsync(uidBuffer, CancellationToken.None);
var uid = Encoding.UTF8.GetString(uidBuffer, 0, uidResult.Count);
Console.WriteLine($"UID recebido: {uid}");
Console.WriteLine("Aguardando mensagens...\n");

var receiveBuffer = new byte[64 * 1024];

while (client.State == WebSocketState.Open)
{
    using var ms = new System.IO.MemoryStream();
    WebSocketReceiveResult result;

    do
    {
        result = await client.ReceiveAsync(receiveBuffer, CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Fechando", CancellationToken.None);
            Console.WriteLine("Conexão encerrada pelo servidor.");
            return;
        }

        ms.Write(receiveBuffer, 0, result.Count);
    }
    while (!result.EndOfMessage);

    var data = ms.ToArray();

    Console.WriteLine($"[Texto] {Encoding.UTF8.GetString(data)}");
}
