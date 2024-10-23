using AsyncPrograming.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AsyncPrograming.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TestsController : ControllerBase
  {
    private readonly ILogger<TestsController> logger;
    private readonly HttpClient client = new HttpClient();
    private readonly IAsyncRequest asyncRequest;

    public TestsController(ILogger<TestsController> logger, IAsyncRequest asyncRequest)
    {
      this.logger = logger;
      this.asyncRequest = asyncRequest;
    }

    [HttpGet("sync")]
    public IActionResult SyncRequest()
    {
      Thread.Sleep(3000); // Main Thread Thread Spleep ile Bloke edildi.
      this.logger.LogInformation($"Sync Request Thread {Thread.CurrentThread.ManagedThreadId}");
      return Ok();
    }

    [HttpGet("async")]
    public async Task<IActionResult> RequestAsync()
    {
      // Task Run bir operasyonun asenkron çalışmasını sağlar
      // Senkron methodlarımızı Task.Run ile asenkron hale getirebiliriz.
      // Task.Run illa thread pool üzerindne Main Thread dışında başka bir Thread alacak diye bir kaide yok, işlem uzun sürerse genelde Thread Pool içerisindeki farklı bir thread kullanır.
      // Ama işlemin hızlı bir şekilde sonuçlanacağını biliyorsa, Main Thread Bloke etmeden Main Thread üzerinde de çalışabilir.

      await Task.Run(() =>
      {
          Task.Delay(3000);
      });

       // Main Thread Non-Blocking
      this.logger.LogInformation($"Async Request Thread {Thread.CurrentThread.ManagedThreadId}");
      return Ok();
    }


    [HttpGet("httpClientAsync")]
    public async Task<IActionResult> HttpClientAsync()
    {
      // asenkron bir çağırının ne zaman biteceğini bilmediğimizden cevabı alıp işlem yapmak için await ile isteği uyutuyoruz.
      var data = await client.GetStringAsync("https://www.google.com");

      this.logger.LogInformation($"HttpClientAsync Thread {Thread.CurrentThread.ManagedThreadId}");

      return Ok(data);

    }


    // Asenkron bir çağrıyı senkron olarak çalıştırmak için
    [HttpGet("httpClientSync")]
    public async Task<IActionResult> HttpClient()
    {

      // await yerine Result yazılırsa asenkron olan bir kod bloğunu senkronlaştırıp sonuc olarak requesti bloklamış oluruz. Main Thread üzerinden çalışır.
      var data =  client.GetStringAsync("https://www.google.com").Result;
      var data2 = client.GetStringAsync("https://www.google.com").GetAwaiter().GetResult(); // Bu kod bloğuda isteği bloke eder.

      this.logger.LogInformation($"HttpClientSync Thread {Thread.CurrentThread.ManagedThreadId}");

      return Ok(data);

    }


    [HttpGet("taskContinueWith")]
    public IActionResult TaskContinueWith()
    {

      // bir kod blogunun await edilebilmesi için async keyword'e ihtiyacı var.
      // Asenkron kod bloğunda senkron kodlar önceliklidir.
      // await yazıp response uyutmadığımız sürece aşağıdaki gibi full asenkron event güdümlü çalışan bir yapıda kod sıralaması değişecektir.
      this.logger.LogInformation("İstek Başladı \n");
      
      client.GetStringAsync("https://www.google.com")
        .ContinueWith(async (data) =>
      {
        // data.Result içinde alsında asenkron olarak veri çözüldüğü için burdaki result ana kodu bloke etmez.
        this.logger.LogInformation($"Google Content Length:{data.Result.Length.ToString()} \n");
        this.logger.LogInformation($"TaskContinueWith Thread {Thread.CurrentThread.ManagedThreadId} \n");
      });

      // birbirinde bağımsız 2 farklı asenkron kod bloğu
      client.GetStringAsync("https://neominal.com")
       .ContinueWith(async (data) =>
       {
         // data.Result içinde alsında asenkron olarak veri çözüldüğü için burdaki result ana kodu bloke etmez.
         this.logger.LogInformation($"Neominal Content Length: {data.Result.Length.ToString()} \n");
         this.logger.LogInformation($"TaskContinueWith Thread {Thread.CurrentThread.ManagedThreadId} \n");
       });

      this.logger.LogInformation("İstek Bitti \n");



      return Ok();

    }


    [HttpGet("waitAll")]
    public IActionResult WaitAll()
    {
      var task1 = client.GetStringAsync("https://www.google.com");
      var task2 = client.GetStringAsync("https://neominal.com");

      Task.WaitAny(task1); // task1 çözülene kadar beklet. Buda Main Thread bloke eder.
      Task.WaitAll(task1, task2); // Dikkatli olalım Main Thread Bloke oldu. ve Sıralı Senkron bir işleme döndü.

      Console.Out.WriteLineAsync("Google" + task1.Result.Length.ToString());
      Console.Out.WriteLineAsync("Neominal" + task2.Result.Length.ToString());

      this.logger.LogInformation($"WaitAll Thread {Thread.CurrentThread.ManagedThreadId} \n");

      return Ok();
    }

    [HttpGet("whenAll")]
    public async Task<IActionResult> WhenAll()
    {
      var task1 = client.GetStringAsync("https://www.google.com");
      var task2 = client.GetStringAsync("https://neominal.com");

      
      await Task.WhenAll(task1, task2); // Dikkatli olalım Main Thread Bloke etmez. ve Sıralı Asenkron bir işleme olarak iki taskıda çözer.

      Console.Out.WriteLineAsync("Google" + task1.Result.Length.ToString());
      Console.Out.WriteLineAsync("Neominal" + task2.Result.Length.ToString());

      this.logger.LogInformation($"WhenAll Thread {Thread.CurrentThread.ManagedThreadId} \n");

      return Ok();
    }


    [HttpGet("customAsyncService")]
    public async Task<IActionResult> CustomAsyncService(CancellationToken cancellationToken)
    {

      // Windows Programadaki yönetim
      //CancellationTokenSource c = new CancellationTokenSource();
      //CancellationToken token = c.Token;

      //c.Cancel();
      try
      {
        var response = await this.asyncRequest.HandleAsync(cancellationToken);

        // eğer ki istek iptal durumu meydana gelirse
        return Ok(response);
      }
      catch (OperationCanceledException ex)
      {

        return StatusCode(500,ex.Message);
     
      }

    }


    [HttpGet("AsyncExceptionSample")]
    public async Task<IActionResult> AsyncExceptionSample()
    {

      try
      {
        var task = Task.Run(() => Task.FromException(new Exception("Hata")));

        // Exception durumlarında hata durumlarını yakalamak için kod bloğun bitmesini bekletmemiz lazım. Yoksa exception durumlarını yakalayamayız.
        // await kullanımı önemli
        await task;

      }
      catch (Exception ex)
      {
        await Console.Out.WriteLineAsync(ex.Message);
        throw;
      }

      return Ok();

    }

  }
}
