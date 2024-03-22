namespace Tetris;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        try
        {
            const int framesPerSecond = 60;
            CurrGame = new TetrisGame(10, 20, framesPerSecond, 100);
            gameArea.Drawable = CurrGame;
            var timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000f / framesPerSecond);
            timer.IsRepeating = true;
            timer.Tick += (_, _) =>
            {
                CurrGame.InvokeUpdate();
                gameArea.Invalidate();
            };
            timer.Start();
        } catch (Exception exc) {
            Console.Error.WriteLine("Problems during the game initialization!" + exc);
            throw;
        }
    }

    private TetrisGame CurrGame { get; }
    private void OnMoveLeft(object sender, EventArgs e) => CurrGame.InvokeMoveLeft();
    private void OnMoveRight(object sender, EventArgs e) => CurrGame.InvokeMoveRight();
    private void OnMoveDown(object sender, EventArgs e) => CurrGame.InvokeMoveDown();
    private void OnFlip(object sender, EventArgs e) => CurrGame.InvokeFlip();
}