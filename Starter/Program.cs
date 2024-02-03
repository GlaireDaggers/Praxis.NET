using Starter;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using (var game = new StarterGame())
        {
            game.Run();
        }
    }
}