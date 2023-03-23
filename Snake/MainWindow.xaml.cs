using snake.Games;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Xml.Serialization;

namespace snake.Games
{
    //////  Публичные значение имени и очков для списка рекордов
    public class SnakeHighscore
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
    }
    ////// Публичная значения со размером змейки, кординаты и логическая проверка головы змейки 
    public class SnakePart
    {
        public UIElement UiElement { get; set; }
        public Point Position { get; set; }
        public bool IsHead { get; set; }
    }
}
namespace Snake
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ////// Приватное обявление использование времени системой для игры 
        private System.Windows.Threading.DispatcherTimer gameTickTimer = new System.Windows.Threading.DispatcherTimer();
        ////// Приватное обявление рандома для дальнешего использования
        private Random rnd = new Random();
        ////// Приватное обявление зачения еды в количесте нуливого значения
        private UIElement snakeFood = null;
        //////  Обявление цвета для еды
        private SolidColorBrush foodBrush = Brushes.DeepPink;
        ////// Размер квадрата отображающий змейку на поле
        const int SnakeSquareSize = 20;
        ////// Стартовая длина змейки  
        const int SnakeStartLength = 4;
        ////// Стартовая скорость змейки 
        const int SnakeStartSpeed = 300;
        ////// максимальная скорость змейки 
        const int SnakeSpeedThreshold = 100;
        ////// Обявление цвета для тела и головы змейки
        private SolidColorBrush snakeBodyBrush = Brushes.MediumBlue;
        private SolidColorBrush snakeHeadBrush = Brushes.SaddleBrown;
        ////// Обявление списка для каждой клетки змейки 
        private List<SnakePart> snakeParts = new List<SnakePart>();
        ////// Обявление управляющих клавишь на клавиатуры 
        public enum SnakeDirection { Left, Right, Up, Down };
        ////// ???
        private SnakeDirection snakeDirection = SnakeDirection.Right;
        ////// Длина змейки 
        private int snakeLength;
        ////// ???
        private int currentScore = 0;
        public MainWindow()
        {

            InitializeComponent();
        ////// запуск игрового времени
            gameTickTimer.Tick += GameTickTimer_Tick;
        ////// Загрузка листа с рекордами
            LoadHighscoreList();
        }
        ////// Команда времени вызывающию команду движения каждый тик в ситстеме 
        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        ////// Команда которая запускается при открытие окна 
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ////// Вызов разукрашивания поля
            DrawGameArea();
        }
        ////// Мы обрабатываем список snakeParts в цикле,и для каждого фрагмента мы проверяем, был ли для него назначен UIElement 
        ////// и если нет, мы создаём прямоугольник и добавляем к игровому полю не забывая сохранить ссылку на него в экземпляре SnakePart в свойстве
        ///UiElement для позиционирования самих элементов внутри Canvas'а( игрового Поля).
        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush)
                    };
                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }
        /// Внутри цикла while мы непрерывно создаем экземпляры элемента управления Rectangle
        /// и добавляем их на панель Canvas (Игровое поле). Мы закрашиваем их в клетку цвет выбирайте сами,
        /// а ширина и высота определяются нашей постоянной SnakeSquareSize поскольку мы хотим что бы ячейки были квадратными.
        /// В каждой итерации мы используем nextX и nextY что бы определять момент перехода на следующую строку (достижение правой границы поля) а затем и момент остановки
        /// (когда мы достигли нижней строки И правой границы поля одновременно)
        private void DrawGameArea()
        {
            bool doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while (doneDrawingBackground == false)
            {
                Rectangle rect = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? Brushes.ForestGreen : Brushes.ForestGreen
                };
                GameArea.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnakeSquareSize;
                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if (nextY >= GameArea.ActualHeight)
                    doneDrawingBackground = true;
            }

        }
        ////// команда для передвижение змейки
        private void MoveSnake()
        {
            ////// Удалите последнюю часть змеи, готовя новую часть, добавленную ниже
            while (snakeParts.Count >= snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }
            ////// Далее мы добавим к змее новый элемент, которым будет (новая) голова
            ////// Мы помечаем все существующие детали как элементы, не относящиеся к голове (телу)   
            foreach (SnakePart snakePart in snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }
            ////// Определите, в каком направлении развернуть змею, основываясь на текущем направлении
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }
            ////// Теперь добавьте новую часть головы в наш список частей змеи
            snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });
            ////// Вызов Создание фрагмета змейки  
            DrawSnake();
            ////// Вызов команды столкновения стенки тела змейки 
            DoCollisionCheck();
        }
        ////// Команда для обявление новой позиции еды На игровом поле
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
            int foodX = rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = rnd.Next(0, maxY) * SnakeSquareSize;
            ////// Проверка чтобы не не поставить еду на саму змейку
            foreach (SnakePart snakePart in snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }
            return new Point(foodX, foodY);
        }
        ////// Команда для добавление еды на игровое поле 
        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse()
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = foodBrush
            };
            GameArea.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }
        ////// Запуск игры
        private void StartNewGame()
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;

        ////// Обновляем игровое поле и убираем лишьние 
            foreach (SnakePart snakeBodyPart in snakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
            }
            snakeParts.Clear();
            if (snakeFood != null)
                GameArea.Children.Remove(snakeFood);
        ////// Сброс настроек
            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Right;
            snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);
        //////// Создание новой змеи и еду
            DrawSnake();
            DrawSnakeFood();
        ////// Обновление игрового статуса 
            UpdateGameStatus();
        ////// Запук внутри игрового времени      
            gameTickTimer.IsEnabled = true;
        }
        ////// События с проверкой нажатие клавишь для упавления. 
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch (e.Key)
            {
                case Key.Up:
                    if (snakeDirection != SnakeDirection.Down)
                        snakeDirection = SnakeDirection.Up;
                    break;
                case Key.Down:
                    if (snakeDirection != SnakeDirection.Up)
                        snakeDirection = SnakeDirection.Down;
                    break;
                case Key.Left:
                    if (snakeDirection != SnakeDirection.Right)
                        snakeDirection = SnakeDirection.Left;
                    break;
                case Key.Right:
                    if (snakeDirection != SnakeDirection.Left)
                        snakeDirection = SnakeDirection.Right;
                    break;
                case Key.Space:
                    DrawGameArea();
                    StartNewGame();
                    break;
            }
            if (snakeDirection != originalSnakeDirection)
                MoveSnake();
        }
        ////// Проверка столкновения змейки с стенкой и хвостом и едой 
        private void DoCollisionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            ////// Столкновение с едой 
            if ((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
            {
                EatSnakeFood();
                return;
            }
            ////// Столкновение с стенкой границами поля 
            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) ||
            (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                EndGame();
            }
            ////// Столкновение с телом змеи
            foreach (SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                    EndGame();
            }
        }
        ////// Команда для поядание еды
        private void EatSnakeFood()
        {
            ////// Увелечения змейки и очков
            snakeLength++;
            currentScore++;
            ////// ускороние змейки 
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(snakeFood);
            ////// Добавление еды на поле 
            DrawSnakeFood();
            ////// обновление игрового статуса 
            UpdateGameStatus();
        }
        ////// Обновление значений на табло игры 
        private void UpdateGameStatus()
        {
            this.tbStatusScore.Text = currentScore.ToString();
            this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }
        ////// Обявление конец игры
        private void EndGame()
        {
            bool isNewHighscore = false;
            if (currentScore > 0)
            {
                int lowestHighscore = (this.HighscoreList.Count > 0 ? this.HighscoreList.Min(x => x.Score) : 0);
                if ((currentScore > lowestHighscore) || (this.HighscoreList.Count < MaxHighscoreListEntryCount))
                {
                    bdrNewHighscore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;
                }
            }
            if (!isNewHighscore)
            {
                tbFinalScore.Text = currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }
            gameTickTimer.IsEnabled = false;

        }
        //////???
        public ObservableCollection<SnakeHighscore> HighscoreList
        { get; set; } = new ObservableCollection<SnakeHighscore>();
        //////???
        private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }
        ////// Максимум количество для списка попыток
        int MaxHighscoreListEntryCount = 5;
        //////???
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void LoadHighscoreList()
        {
            if (File.Exists("snake_list.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
                using (Stream reader = new FileStream("snake_list.xml", FileMode.Open))
                {
                    List<SnakeHighscore> tempList = (List<SnakeHighscore>)serializer.Deserialize(reader);
                    this.HighscoreList.Clear();
                    foreach (var item in tempList.OrderByDescending(x => x.Score))
                        this.HighscoreList.Add(item);
                }
            }
        }
        private void SaveHighscoreList()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighscore>));
            using (Stream writer = new FileStream("snake_list.xml", FileMode.Create))
            {
                serializer.Serialize(writer, this.HighscoreList);
            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;
            // Where should the new entry be inserted?
            if ((this.HighscoreList.Count > 0) && (currentScore < this.HighscoreList.Max(x => x.Score)))
            {
                SnakeHighscore justAbove = this.HighscoreList.OrderByDescending(x => x.Score).First(x => x.Score >= currentScore);
                if (justAbove != null)
                    newIndex = this.HighscoreList.IndexOf(justAbove) + 1;
            }
            // Create & insert the new entry
            this.HighscoreList.Insert(newIndex, new SnakeHighscore()
            {
                PlayerName = txtPlayerName.Text,
                Score = currentScore
            });
            // Make sure that the amount of entries does not exceed the maximum
            while (this.HighscoreList.Count > MaxHighscoreListEntryCount)
                this.HighscoreList.RemoveAt(MaxHighscoreListEntryCount);

            SaveHighscoreList();

            bdrNewHighscore.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }
    }
}
