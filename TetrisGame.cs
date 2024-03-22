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
        Matrix       = new Color[height][];
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
    public float Speed { get; set; }

    internal Color[][] Matrix { get; set; } // x,y!
    private int Score { get; set; }
    private int HighScore { get; set; }
    private TetrisElement CurrentElement { get; set; }
    private bool GameOver { get; set; }
    private float InitialSpeed { get; }
    private Color BackdropColor { get; } = new Color(255, 255, 255, 125);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        Render(canvas);

        const int fontSize = 15;
        canvas.FillColor = BackdropColor;
        canvas.FillRectangle(0, 0, Width * CellSize, fontSize + 5);
        canvas.FontColor = Colors.Black;
        canvas.FontSize  = fontSize;
        canvas.DrawString($"Score: {Score}", 0, fontSize, HorizontalAlignment.Left);
        canvas.DrawString($"Highscore: {HighScore}", Width * CellSize, fontSize, HorizontalAlignment.Right);

        if (!GameOver) return;
        canvas.FillColor = BackdropColor;
        canvas.FillRectangle(0, 0, Width * CellSize, Height * CellSize);
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
        {
            Matrix[y] = new Color[Width];
            for (var x = 0; x < Width; x++)
                Matrix[y][x] = Colors.Transparent;
        }
    }

    private void Render(ICanvas canvas)
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var cell = Matrix[y][x];
            if (CurrentElement?.IsDone == false && CurrentElement?.Contains(x, y) == true)
                cell = CurrentElement.Fill;
            if (cell.Alpha == 0) cell = Colors.Gray;
            canvas.FillColor = cell;
            canvas.FillRectangle(x * CellSize, y * CellSize, CellSize, CellSize);
            canvas.StrokeColor = Colors.Black;
            canvas.DrawRectangle(x * CellSize, y * CellSize, CellSize, CellSize);
        }
    }

    private TetrisElement GetNewElement()
    {
        var form    = GameConstants.Forms[new Random().Next(GameConstants.Forms.Length)];
        var fill    = 1 + new Random().Next(GameConstants.Fills.Length - 1);
        var element = new TetrisElement(form, GameConstants.Fills[fill]);
        element.Rotate(new Random().Next(4));
        element.X      = new Random().Next(Width - element.Width + 1);
        CurrentElement = element;
        var collision                  = element.Collides(this, 0, 0);
        if (collision.matrix) GameOver = true;
        return element;
    }

    private float UpdatesPerSecond => Fps / (Speed / 100f);

    /// <summary>Is called from outside in the <see cref="Fps" /> rate and calls <see cref="Update" /> in the <see cref="Speed" /> rate.</summary>
    public void InvokeUpdate()
    {
        _framesSinceLastUpdate++;
        if (_framesSinceLastUpdate < UpdatesPerSecond) return;
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
        CurrentElement.Rotate(1);
        if (CurrentElement.X + CurrentElement.Width > Width)
            CurrentElement.X = Width - CurrentElement.Width;
        if (CurrentElement.Collides(this, 0, 0).matrix)
            CurrentElement.Rotate(-1);
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
                if (Matrix[y][x].Alpha == 0)
                {
                    complete = false;
                    break;
                }

            if (!complete) continue;
            for (var y2 = y; y2 > 0; y2--)
            for (var x = 0; x < Width; x++)
                Matrix[y2][x] = Matrix[y2 - 1][x];
            for (var x = 0; x < Width; x++)
                Matrix[0][x] = Colors.Transparent;
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

public class TetrisElement(int[][] form, Color fill)
{
    private int[][] Form { get; set; } = form;
    public Color Fill { get; } = fill;
    public int Width { get; private set; } = form.Max(row => row.Length);
    public int Height { get; private set; } = form.Length;
    public int X { get; set; }
    public int Y { get; internal set; }
    public bool IsDone { get; private set; }
    private int Right => X + Width;
    private int Bottom => Y + Height;

    private void WriteToMatrix(TetrisGame game)
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
                matrix[absY][absX] = Fill;
            }
        }
    }

    public bool Contains(int x, int y)
    {
        var relativeY = y - Y;
        var relativeX = x - X;
        if (relativeY < 0) return false;
        if (relativeX < 0) return false;
        if (relativeY >= Height) return false;
        if (relativeX >= Width) return false;
        var formValue = Form[relativeY][relativeX];
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
            var absY      = Y + y + directionY;
            var absX      = X + x + directionX;
            var formValue = Form[y][x];
            if (formValue <= 0) continue;
            if (absY >= game.Height || absX >= game.Width) continue;
            if (absY < 0 || absX < 0) continue;
            if (matrix[absY][absX].Alpha > 0)
                return (true, false, false, false, false);
        }

        return (false, false, false, false, false);
    }

    public void Rotate(int direction)
    {
        if (direction == 0) return;

        for (var r = 0; r < Math.Abs(direction); r++)
        {
            Form            = direction > 0 ? RotateClockwise(Form) : RotateCounterClockwise(Form);
            (Width, Height) = (Height, Width);
        }
    }

    private static int[][] RotateClockwise(int[][] original)
    {
        var n       = original.Length;
        var m       = original[0].Length;
        var rotated = new int[m][];
        for (var i = 0; i < m; i++)
        {
            rotated[i] = new int[n];
            for (var j = 0; j < n; j++)
                rotated[i][j] = original[n - j - 1][i];
        }

        return rotated;
    }

    private static int[][] RotateCounterClockwise(int[][] original)
    {
        var n       = original.Length;
        var m       = original[0].Length;
        var rotated = new int[m][];
        for (var i = 0; i < m; i++)
        {
            rotated[i] = new int[n];
            for (var j = 0; j < n; j++)
                rotated[i][j] = original[j][m - i - 1];
        }

        return rotated;
    }

    public void Update(TetrisGame game)
    {
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
    private static readonly int[][] Form1 = [[1, 1, 1], [1, 0, 0]];
    private static readonly int[][] Form2 = [[1, 1, 1], [0, 1, 0]];
    private static readonly int[][] Form3 = [[1, 1, 1]];
    private static readonly int[][] Form4 = [[1, 1], [1, 1]];
    private static readonly int[][] Form5 = [[1, 1, 0], [0, 1, 1]];

    public static Color[] Fills { get; } =
    [
        Colors.Transparent, Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.Cyan
    ];

    public static int[][][] Forms { get; } = [Form1, Form2, Form3, Form4, Form5];
}