// See https://aka.ms/new-console-template for more information
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;

var connectionString = "User ID=postgres;Password=password;Host=localhost;Port=5432;Database=Sample;Pooling=true;";

var services = new ServiceCollection()
  .AddDbContextPool<DbContext, MyDbContext>(o => o.UseNpgsql(connectionString))
  .BuildServiceProvider();
{
  using var scope1 = services.CreateScope();
  var dbContext = scope1.ServiceProvider.GetRequiredService<DbContext>();
  Console.WriteLine("Using context: {0}", dbContext.ContextId);
  await dbContext.Database.EnsureCreatedAsync();
  await dbContext.Database.MigrateAsync();
  dbContext.AddRange(new Blog { Name = "n1", Posts = new List<Post> { new Post { Date = DateTime.UtcNow, } } },
    new Blog { Name = "n2", Posts = new List<Post> { new Post { Date = DateTime.UtcNow, } } },
    new Blog { Name = "n3", Posts = new List<Post> { new Post { Date = DateTime.UtcNow, } } });

  dbContext.SaveChanges();
}

while (true)
{
  var cts2 = new CancellationTokenSource();
  cts2.CancelAfter(4);
  try
  {
    await Get(cts2.Token);
    Console.WriteLine("all is good");
  }
  catch (Exception e)
  {
    Console.WriteLine(e.ToString());
    Console.WriteLine("something cancelled");
  }

  await Get(CancellationToken.None);
}

async Task Get(CancellationToken cancellationToken)
{
  using var scope1 = services.CreateScope();
  var dbContext = scope1.ServiceProvider.GetRequiredService<DbContext>();
  Console.WriteLine("Using context: {0}", dbContext.ContextId);
  await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

  _ = dbContext.Set<Blog>().Future();

  var posts = dbContext.Set<Post>().Future();
  Console.WriteLine("Obtaining...");
  var allPosts = await posts.ToListAsync(cancellationToken);

  Console.WriteLine("Obtained {0}", allPosts.Count);
}

public class MyDbContext : DbContext
{
  public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
  {
  }

  public DbSet<Blog> Blogs { get; set; }

  public DbSet<Post> Posts { get; set; }
}

public class Blog
{
  public int Id { get; set; }

  public string Name { get; set; }

  public ICollection<Post> Posts { get; set; }
}

public class Post
{
  public int Id { get; set; }

  public DateTime Date { get; set; }

  public int BlogId { get; set; }
}
