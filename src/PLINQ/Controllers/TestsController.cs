using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLINQ.Models;

namespace PLINQ.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TestsController : ControllerBase
  {
    private readonly NorthwndContext  db = new NorthwndContext();

    [HttpGet("asParalel")]
    public IActionResult AsParalel()
    {
      Func<int, bool> lamda = (x) =>
      {
        return x % 2 == 0;
      };

      // ParallelExecutionMode.ForceParallelism Paralel işleme zorlar.
      // WithExecutionMode(ParallelExecutionMode.ForceParallelism)

      Enumerable.Range(0, 1000).AsParallel().WithDegreeOfParallelism(4).Where(lamda).ForAll(x=>
      {
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine("x :" + x);

      });

      return Ok();
    }


    [HttpGet("asOrdered")]
    public IActionResult AsOrdered()
    {
  
      // AsOrdered sıralama işlemi performansı etkilen bir durum. Sıralı olarak gelmesi önemli değilse paralel olarak tanımlanması daha mantıklı.

      var paralel = Enumerable.Range(0, 1000).AsParallel().AsOrdered().WithDegreeOfParallelism(4);

      paralel.ToList().ForEach(x =>
      {
        Console.WriteLine("x" + x);
      });

      return Ok();
    }


    [HttpGet("asException")]
    public IActionResult AsException()
    {

      // AsOrdered sıralama işlemi performansı etkilen bir durum. Sıralı olarak gelmesi önemli değilse paralel olarak tanımlanması daha mantıklı.

      try
      {
        // LINQ sorgusu olduğu için birden fazla item içerisinde bir exception durumu oluşabilir. Bu sebeple hataları AggregationException sınıfı ile yakalamamız gerekir. 
        Enumerable.Range(0, 1000).AsParallel().Where(x => x / 0 == 0).ForAll(x =>
        {
          Console.WriteLine("x" + x);
        });
      }
      catch (AggregateException ex)
      {
        ex.InnerExceptions.ToList().ForEach(item =>
        {
          Console.WriteLine(item.Message);
        });
      }
    
      return Ok();
    }


    [HttpGet("asCancelation")]
    public IActionResult AsCancelation(CancellationToken cancellationToken)
    {

      // AsOrdered sıralama işlemi performansı etkilen bir durum. Sıralı olarak gelmesi önemli değilse paralel olarak tanımlanması daha mantıklı.

      try
      {
        // LINQ sorgusu olduğu için birden fazla item içerisinde bir exception durumu oluşabilir. Bu sebeple hataları AggregationException sınıfı ile yakalamamız gerekir. 
        Enumerable.Range(0, 1000).AsParallel().WithCancellation(cancellationToken).ForAll(x =>
        {
          Thread.SpinWait(500000); // 50,000 lik döngü

          Console.WriteLine("x" + x);
        });
      }
      catch (OperationCanceledException ex) // Sorgunun düzgün çalışıp request iptal edildiği durum için.
      {
        Console.WriteLine(ex.Message);
      }

      return Ok();
    }


    [HttpGet("asParalelDatabase")]
    public IActionResult AsParalelDatabase(CancellationToken cancellationToken)
    {

      db.Products.Include(x => x.Category).Include(x => x.Supplier).Where(x => x.ProductName.Contains("Ch")).AsParallel().ForAll(item =>
      {
        // Parelel olarak yönetilen kod bloğu burası oluyor.

        Console.WriteLine($"ProductName :${item.ProductName} CategoryName : ${item.Category.CategoryName}");

        Console.WriteLine("Thread :" + Thread.CurrentThread.ManagedThreadId);
        // verileri başka bir sisteme entegre et.
        // SAP gönder.
        // API Tek tek ilet
        // Siparişleri çekip Faturalarını Bas.
      });

      return Ok();
    }
  }
}
