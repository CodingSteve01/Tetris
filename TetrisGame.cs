namespace Tetris;

public class TetrisGame : IDrawable
{
    private const int CellSize = 20;
    private int _framesSinceLastUpdate;

    public TetrisGame(int width, int height, int framesPerSecond, float speed)
    {
        Width        = width;
        Height       = height;
        Fps          = framesPerSecond;
        InitialSpeed = speed;
        Speed        = speed;
        Matrix       = new Color[height, width];
        InitMatrix();
        CurrentElement = GetNewElement();
        Score          = 0;
        HighScore      = 0;
        GameOver       = false;
    }

    internal int Width { get; }
    internal int Height { get; }
    private int Fps { get; }

    /// <summary>The speed percentual (0-100) of the <see cref="Fps" /> rate.</summary>
    private float Speed { get; set; }

    internal Color[,] Matrix { get; set; } // x,y!
    private int Score { get; set; }
    private int HighScore { get; set; }
    private TetrisElement CurrentElement { get; set; }
    public bool GameOver { get; set; }
    private float InitialSpeed { get; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        Render(canvas);
        canvas.FontColor = Colors.Black;
        canvas.FontSize  = 20;
        canvas.DrawString($"Score: {Score}", 0, 0, HorizontalAlignment.Left);
        canvas.DrawString($"Highscore: {HighScore}", Width * CellSize, 0, HorizontalAlignment.Right);

        if (!GameOver) return;
        canvas.FontSize  = 40;
        canvas.FontColor = Colors.Red;
        canvas.DrawString("Game Over", Width * CellSize / 2f, Height * CellSize / 2f, HorizontalAlignment.Center);
    }

    private void StartOver()
    {
        Score    = 0;
        Speed    = InitialSpeed;
        GameOver = false;
        InitMatrix();
        CurrentElement = GetNewElement();
    }

    private void InitMatrix()
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            Matrix[y, x] = Colors.Transparent;
    }

    private void Render(ICanvas canvas)
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var cell = Matrix[y, x];
            if (CurrentElement?.Contains(x, y) == true)
                cell = CurrentElement.Fill;
            if (cell.Alpha == 0) continue;
            canvas.FillColor = cell;
            canvas.FillRectangle(x * CellSize, y * CellSize, CellSize, CellSize);
        }
    }

    private TetrisElement GetNewElement()
    {
        var form    = GameConstants.Forms[new Random().Next(GameConstants.Forms.Length)];
        var fill    = 1 + new Random().Next(GameConstants.Fills.Length - 1);
        var element = new TetrisElement(form, GameConstants.Fills[fill]);
        element.Rotate(this, new Random().Next(4));
        element.X      = new Random().Next(Width - element.Width + 1);
        CurrentElement = element;
        var collision                  = element.Collides(this, 0, 0);
        if (collision.matrix) GameOver = true;
        return element;
    }

    /// <summary>Is called from outside in the <see cref="Fps" /> rate and calls <see cref="Update" /> in the <see cref="Speed" /> rate.</summary>
    public void InvokeUpdate()
    {
        _framesSinceLastUpdate++;
        if (!(_framesSinceLastUpdate >= Fps * Speed / 100f)) return;
        _framesSinceLastUpdate = 0;
        Update();
    }

    public void InvokeMoveLeft()
    {
        if (GameOver)
        {
            StartOver();
            return;
        }

        // Move the element left if there's no collision.
        if (CurrentElement.Collides(this, -1, 0).matrix || CurrentElement.X - 1 < 0) return;
        CurrentElement.X--;
    }

    public void InvokeMoveRight()
    {
        if (GameOver)
        {
            StartOver();
            return;
        }

        // Move the element right if there's no collision.
        if (CurrentElement.Collides(this, 1, 0).matrix || CurrentElement.X + 1 > Width - CurrentElement.Width) return;
        CurrentElement.X++;
    }

    public void InvokeFlip()
    {
        if (GameOver)
        {
            StartOver();
            return;
        }

        // Rotate the element if there's no collision.
        CurrentElement.Rotate(this, 1);
        if (CurrentElement.X + CurrentElement.Width > Width)
            CurrentElement.X = Width - CurrentElement.Width;
        if (CurrentElement.Collides(this, 0, 0).matrix)
            CurrentElement.Rotate(this, -1);
    }

    public void InvokeMoveDown()
    {
        if (GameOver)
        {
            StartOver();
            return;
        }

        // Move the element down faster if there's no collision.
        if (CurrentElement.Collides(this, 0, 1).matrix || CurrentElement.Y + 1 > Height - CurrentElement.Height) return;
        CurrentElement.Y++;
    }

    private void Update()
    {
        if (GameOver) return;
        if (CurrentElement.IsDone)
            CurrentElement = GetNewElement();
        CurrentElement.Update(this);
        ClearCompleteRows();
    }

    private void ClearCompleteRows()
    {
        var rowsCleared = 0;
        for (var y = Height - 1; y >= 0; y--)
        {
            var complete = true;
            for (var x = 0; x < Width; x++)
                if (Matrix[y, x].Alpha == 0)
                {
                    complete = false;
                    break;
                }

            if (!complete) continue;
            for (var y2 = y; y2 > 0; y2--)
            for (var x = 0; x < Width; x++)
                Matrix[y2, x] = Matrix[y2 - 1, x];
            for (var x = 0; x < Width; x++)
                Matrix[0, x] = Colors.Transparent;
            y++;
            rowsCleared++;
        }

        if (rowsCleared <= 0) return;
        Score += rowsCleared;
        Speed =  Math.Max(100, Speed + 5);

        if (Score <= HighScore) return;
        HighScore = Score;
    }
}

public class TetrisElement(int[,] form, Color fill)
{
    public int[,] Form { get; private set; } = form;
    public Color Fill { get; } = fill;
    public int Width { get; private set; } = form.GetLength(0);
    public int Height { get; private set; } = form.GetLength(1);
    public int X { get; set; }
    public int Y { get; internal set; }
    public bool IsDone { get; set; }
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public void WriteToMatrix(TetrisGame game)
    {
        var matrix = game.Matrix;
        for (var y = 0; y < Height; y++)
        {
            var absY = Y + y;
            if (absY >= game.Height) continue;
            for (var x = 0; x < Width; x++)
            {
                var absX = X + x;
                if (absX >= game.Width) continue;
                if (!Contains(absX, absY)) continue;
                matrix[absY, absX] = Fill;
            }
        }
    }

    public bool Contains(int x, int y)
    {
        var relativeX = x - X;
        var relativeY = y - Y;
        if (relativeX < 0) return false;
        if (relativeY < 0) return false;
        if (relativeX >= Width) return false;
        if (relativeY >= Height) return false;
        var formValue = Form[relativeX, relativeY];
        return formValue > 0;
    }

    public (bool matrix, bool left, bool right, bool top, bool bottom) Collides(TetrisGame game, int directionX, int directionY)
    {
        if (Bottom + directionY > game.Height)
            return (true, false, false, false, true);
        if (Y + directionY < 0)
            return (true, false, false, true, false);
        if (X + directionX < 0)
            return (true, true, false, false, false);
        if (Right + directionX > game.Width)
            return (true, false, true, false, false);

        var matrix = game.Matrix;
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var absX      = X + x + directionX;
            var absY      = Y + y + directionY;
            var formValue = Form[y, x];
            if (formValue <= 0) continue;
            if (matrix[absY, absX].Alpha > 0)
                return (true, false, false, false, false);
        }

        return (false, false, false, false, false);
    }

    public void Rotate(TetrisGame game, int direction)
    {
        if (direction == 0) return;

        for (var r = 0; r < Math.Abs(direction); r++)
        {
            Form            = direction > 0 ? RotateClockwise(Form) : RotateCounterClockwise(Form);
            (Width, Height) = (Height, Width);
        }
    }

    private static int[,] RotateClockwise(int[,] original)
    {
        var originalWidth  = original.GetLength(0);
        var originalHeight = original.GetLength(1);
        var rotated        = new int[originalHeight, originalWidth];

        for (var i = 0; i < originalWidth; ++i)
        for (var j = 0; j < originalHeight; ++j)
            rotated[j, originalWidth - i - 1] = original[i, j];

        return rotated;
    }

    private static int[,] RotateCounterClockwise(int[,] original)
    {
        var originalWidth  = original.GetLength(0);
        var originalHeight = original.GetLength(1);
        var rotated        = new int[originalHeight, originalWidth];

        for (var i = 0; i < originalWidth; ++i)
        for (var j = 0; j < originalHeight; ++j)
            rotated[originalHeight - j - 1, i] = original[i, j];

        return rotated;
    }

    public void Update(TetrisGame game)
    {
        var matrix = game.Matrix;

        if (IsDone) return;

        var speed = 1;
        var newY  = Y + speed;

        // Collision detection
        var collision = Collides(game, 0, speed);
        if (collision.matrix || collision.bottom)
        {
            WriteToMatrix(game);
            IsDone = true;
            return;
        }

        // Falling down
        Y = newY;
    }
}

public static class GameConstants
{
    private static readonly int[,] Form1 = { { 1, 1, 1 }, { 1, 0, 0 } };
    private static readonly int[,] Form2 = { { 1, 1, 1 }, { 0, 1, 0 } };
    private static readonly int[,] Form3 = { { 1, 1, 1 } };
    private static readonly int[,] Form4 = { { 1, 1 }, { 1, 1 } };
    private static readonly int[,] Form5 = { { 1, 1, 0 }, { 0, 1, 1 } };

    public static Color[] Fills { get; } =
    [
        Colors.Transparent, Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.Cyan
    ];

    public static int[][,] Forms { get; } = { Form1, Form2, Form3, Form4, Form5 };
}