using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using TPLApp.Services;
using static Aspose.Pdf.CollectionItem;

namespace TPLApp.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TestsController : ControllerBase
  {
    private ILogger<TestsController> logger;
    private readonly IPdfGenerator pdfGenerator;

    public TestsController(ILogger<TestsController> logger, IPdfGenerator pdfGenerator)
    {
      this.logger = logger;
      this.pdfGenerator = pdfGenerator;
    }

    // Paralel.For ve Paralel.Foreach 

    [HttpGet("paralelFor")]
    public IActionResult ParalelFor()
    {

      var array = Enumerable.Range(0, 1000);

      // Main Thread üzerinde Parallel sınıfı çalışır fakat içindeki kodlar multi-thread

      // bu sayede Parallel sınıfı için sistemi bloke etmedik.

      Parallel.For(0, array.Count(), (index) =>
      {
        // Buradaki kodlar farklı thread üzerinden paralelde bölünürler.
        this.logger.LogInformation($"index: {index}");
        this.logger.LogInformation($"Thread: {Thread.CurrentThread.ManagedThreadId}");
      });


      array.ToList().ForEach(item =>
      {
        this.logger.LogInformation($"index: {item}");
        this.logger.LogInformation($"Main Thread: {Thread.CurrentThread.ManagedThreadId}");
      });


      return Ok();
    }


    [HttpGet("paralelForAsync")]
    public IActionResult ParalelForAsync(CancellationToken cancellationToken)
    {

      var array = Enumerable.Range(0, 1000);

      // Main Thread üzerinde Parallel sınıfı çalışır fakat içindeki kodlar multi-thread

      // bu sayede Parallel sınıfı için sistemi bloke etmedik.


      try
      {

        Parallel.ForAsync(0, 20, async (index, cancellationToken) =>
        {
          // Buradaki kodlar farklı thread üzerinden paralelde bölünürler.
          this.logger.LogInformation($"index: {index}");
          this.logger.LogInformation($"Thread: {Thread.CurrentThread.ManagedThreadId}");

          cancellationToken.ThrowIfCancellationRequested();

        });
      }
      catch (OperationCanceledException ex)
      {

        throw;
      }


      return Ok();
    }


    [HttpGet("paralelForeach")]
    public IActionResult ParalelForeach()
    {
      string folderName = Path.Combine(Directory.GetCurrentDirectory(),"Files");

      var array = Enumerable.Range(0, 50);

      // Main Thread üzerinde Parallel sınıfı çalışır fakat içindeki kodlar multi-thread

      // bu sayede Parallel sınıfı için sistemi bloke etmedik.

      Stopwatch sp = new Stopwatch();
      sp.Start();

      // [20] [20] [40] [20]

      //Parallel.ForEach(array,(value) =>
      //{
      //  // Buradaki kodlar farklı thread üzerinden paralelde bölünürler.
      //  // Yoğun bir işlem CPU bounded veya IO bounded bir işlem.
      //  pdfGenerator.Generate($"{folderName}/{value}.pdf");


      //  this.logger.LogInformation($"index: {value}");
      //  this.logger.LogInformation($"Thread: {Thread.CurrentThread.ManagedThreadId}");
      //});


      array.ToList().ForEach(item =>
      {
        pdfGenerator.GenerateAsync($"{folderName}01/_{item}.pdf");

        this.logger.LogInformation($"index: {item}");
        this.logger.LogInformation($"Main Thread: {Thread.CurrentThread.ManagedThreadId}");
      });



      sp.Stop();
      this.logger.LogInformation($"Toplam Süre Paralel: {sp.ElapsedMilliseconds}");


      return Ok();
    }


    [HttpGet("raceCondition")]
    public async Task<IActionResult> RaceCondition(CancellationToken token)
    {
      var numbers = Enumerable.Range(0, 100000);
      int counter = 0;
      int total = 0;

      // hesaplama işlemi gibi value döndüreceğimiz bir durum dışında bir diziye değer ekleme gibi çıkan sonucu alma durumumuz varsa
      List<string> names = [];
      // bunu kitleyerek
      object _lockObject = new object();


      // request sonlandırma işlemleri için ParallelOptions üzerinden çalışabiliriz.

      var options = new ParallelOptions();
      options.CancellationToken = token;

      await Parallel.ForAsync(0, numbers.Count(), options, async (index, token) =>
      {

        if(options.CancellationToken.IsCancellationRequested)
        {
          this.logger.LogInformation("İstek İptal edildi");
        }

        options.CancellationToken.ThrowIfCancellationRequested();

        Interlocked.Increment(ref counter);
        Interlocked.Add(ref total, index);
      
        lock(_lockObject) // Thread Safe kitleme işlemi içerisinde
        {
          names.Add(index.ToString());
        }


        await Task.Run(() =>
        {
           Task.Delay(5000);
        });

        // Race Condition durumuna sebep oldu.
        // counter += 1;
        // names.Add(index.ToString());

      });



      return Ok(new { counter, total, names});
    }




  }
}
