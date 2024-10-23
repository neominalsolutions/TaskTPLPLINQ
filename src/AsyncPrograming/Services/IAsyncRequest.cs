namespace AsyncPrograming.Services
{
  // CancellationToken istek sonlarınmak için kullanılan bir token değeri
  public interface IAsyncRequest
  {
    Task<Response> HandleAsync(CancellationToken cancellationToken);
  }

  public class Response
  {
    public string Message { get; set; }

  }

  public class AsyncRequest : IAsyncRequest
  {
    public  Task<Response> HandleAsync(CancellationToken cancellationToken)
    {
      // Hata veya Iptal veya Result döndürebiliriz.
      //Task.FromCanceled(cancellationToken);
      //Task.FromException(new Exception("Hata"));
    

     

      Thread.SpinWait(50000000); // 50000000 satırlık döngü
      // Task.Delay(5000); // 5 snlik bir bekletme yapar ama Thread Bloke etmez.

      // Thread.Sleep(5000); // Bunu kullanmayalım Thread Bloke eder.

       Console.Out.WriteLineAsync("Service Thread :" + Thread.CurrentThread.ManagedThreadId);


      // bu request iptal durumunu yakaladığımız an
      if (cancellationToken.IsCancellationRequested)
      {
        Console.Out.WriteLineAsync("Request iptal edildi");
      }

      // iptal durumu ile ilgili exception OperationCanceledException tipinde bir exception fırlatmamızı sağlar.
      cancellationToken.ThrowIfCancellationRequested();

      return  Task.FromResult(new Response() { Message = "Ok" });
    }
  }
}
