using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// インスタンスのドメイン名とトークンを指定
const string TOKEN = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
const string HOST = "misskey.io";

try{
    //キャンセル用トークンソース
    CancellationTokenSource cts = new CancellationTokenSource();
  
    using(var ws = new ClientWebSocket()){
      
        var uri = new Uri($"wss://{HOST}/streaming?i={TOKEN}");
        await ws.ConnectAsync(uri, CancellationToken.None);

        var uid = Guid.NewGuid().ToString();
      
        //送信用JSON作成
        var json =    
@"{
    ""type"": ""connect"",
    ""body"": {
        ""channel"": ""localTimeline"",
        ""id"": """+ uid + @""" 
    }
}
";
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)),WebSocketMessageType.Text,true,CancellationToken.None);
        var task = Listen(ws,cts.Token);
      
        //エンターキーで終わる
        Console.ReadLine();

      　
        json=
@"{
	""type"": ""disconnect"",
	""body"": {
		""id"": """+ uid + @"""
	}
}"; 
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)),WebSocketMessageType.Text,true,CancellationToken.None);
        await Task.Delay(1000);
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
        await Task.Delay(1000);
        cts.Cancel();
        task.Wait();
        client.Dispose();

        return;
    }
}catch(Exception e){
        Console.WriteLine($"{e.Message}");
}



async Task Listen(WebSocket client, CancellationToken token)
{
    Exception? causedException = null;
    try
    {
        // define buffer here and reuse, to avoid more allocation
        const int chunkSize = 1024 * 4;
        var buffer = new ArraySegment<byte>(new byte[chunkSize]);

        do
        {
            WebSocketReceiveResult result;
            byte[]? resultArrayWithTrailing = null;
            var resultArraySize = 0;
            var isResultArrayCloned = false;
            MemoryStream? ms = null;

            while (true)
            {
                result = await client.ReceiveAsync(buffer, token);
                var currentChunk = buffer.Array;
                var currentChunkSize = result.Count;

                var isFirstChunk = resultArrayWithTrailing == null;
                if (isFirstChunk)
                {
                    // first chunk, use buffer as reference, do not allocate anything
                    resultArraySize += currentChunkSize;
                    resultArrayWithTrailing = currentChunk;
                    isResultArrayCloned = false;
                }
                else if (currentChunk == null)
                {
                    // weird chunk, do nothing
                }
                else
                {
                    // received more chunks, lets merge them via memory stream
                    if (ms == null)
                    {
                        // create memory stream and insert first chunk
                        ms = new MemoryStream();
                        ms.Write(resultArrayWithTrailing!, 0, resultArraySize);
                    }

                    // insert current chunk
                    ms.Write(currentChunk, buffer.Offset, currentChunkSize);
                }

                if (result.EndOfMessage)
                {
                    break;
                }

                if (isResultArrayCloned)
                    continue;

                // we got more chunks incoming, need to clone first chunk
                resultArrayWithTrailing = resultArrayWithTrailing?.ToArray();
                isResultArrayCloned = true;
            }

            ms?.Seek(0, SeekOrigin.Begin);

            var message="";
            var closeFlag=false;
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var data = ms != null ?
                    Encoding.UTF8.GetString(ms.ToArray()) :
                    resultArrayWithTrailing != null ?
                        Encoding.UTF8.GetString(resultArrayWithTrailing, 0, resultArraySize) :
                        null;

                message = data;
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                try{
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK",token);
                }catch{
                    ;
                }
                closeFlag=true;
            }
            else
            {
                if (ms != null)
                {
                    //message ="";
                }
                else
                {
                    Array.Resize(ref resultArrayWithTrailing, resultArraySize);
                    //message ="";
                }
            }

            ms?.Dispose();

            if(closeFlag){
                return;
            }
            WssMessage? wssMessage;
            try{
                wssMessage = JsonSerializer.Deserialize<WssMessage>(message);
                if(wssMessage!=null){
                    var text = wssMessage?.body.body.text??"";
                    Console.WriteLine($"{text}");
                }
            }catch(Exception e){
                Console.WriteLine($"{e.Message}");
            }

        } while (client.State == WebSocketState.Open && !token.IsCancellationRequested);


    }
    catch (TaskCanceledException e)
    {
        // task was canceled, ignore
        causedException = e;
    }
    catch (OperationCanceledException e)
    {
        // operation was canceled, ignore
        causedException = e;
    }
    catch (ObjectDisposedException e)
    {
        // client was disposed, ignore
        causedException = e;
    }
    catch (Exception e)
    {
        causedException = e;
    }
    if(causedException!=null){
        Console.WriteLine(causedException.Message);
    }
}
