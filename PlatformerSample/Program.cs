using PlatformerSample;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using (var game = new PlatformerGame())
        {
            game.Run();
        }
    }
}