using Example;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using (var game = new ExampleGame())
        {
            game.Run();
        }
    }
}