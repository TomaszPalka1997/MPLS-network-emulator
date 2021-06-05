
namespace Router
{
    class Program
    {
        static void Main(string[] args)
        {
            Router router = new Router(args[0]);
            router.Start(); 
         }
    }
}
