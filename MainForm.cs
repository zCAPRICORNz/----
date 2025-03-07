﻿// Правила
// Місце дії цієї гри - «всесвіт» - це розмічена на клітини поверхню або площину - безмежна, обмежена,
// або замкнута (в межі - нескінченна площина).
// Кожна клітина на цій поверхні може перебувати в двох станах: бути «живою» (заповненої) або бути «мертвої» (порожній).
// Клітка може мати до 8 сусідів, що оточують її.
// Розподіл живих клітин на початку гри називається першим поколеніем.Каждое наступне покоління розраховується на основі
// попереднього за такими правилами:
// 1. в порожній (мертвої) клітці, поруч з якою рівно три живі клітини (кількість налаштовується), зароджується життя;
// 2. якщо у живої клітини є дві або три живі сусідки, то ця клітина продовжує жити; в іншому випадку, якщо сусідів менше
// двох або більше трьох, клітина вмирає ( «від самотності» або «від перенаселеності» (кількість налаштовується);
// також в гру введені додаткові умови виживання клітини: максимальний час життя і ризик внезпаной смерті
// Гра припиняється, якщо:
// 1. на поле не залишиться жодної «живої» клітини - РЕАЛІЗОВАНО
// 2. Конфігурація на черговому кроці в точності (без зрушень і поворотів) повторить себе ж на одному з попередніх кроків (складається
// періодична конфігурація) - РЕАЛІЗОВАНО
// 3. При черговому кроці жодна з клітин не змінює свого стану (складається стабільна конфігурація; попереднє правило,
// вироджене до одного кроку назад) - РЕАЛІЗОВАНО
// Ці прості правила призводять до величезного розмаїття форм, які можуть виникнути в грі.
// Гравець не приймає прямої участі в грі, а лише розставляє або генерує початкову конфігурацію «живих» клітин, які потім
// взаємодіють згідно з правилами вже без його
// участі (він є спостерігачем).

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace Game_Of_Life
{
    public partial class MainForm : Form
    {
        private List<string> HistoryHashes = new List<string>();
        /// <summary>
        /// Народилось клітинок
        /// </summary>
        int born = 0;
        /// <summary>
        /// Померло клітинок
        /// </summary>
        int dead = 0;
        /// <summary>
        /// Живих клітинок
        /// </summary>
        int alive = 0;
        /// <summary>
        /// мінімальна кількість сусідів для виживания
        /// </summary>
        int MinNeighbors = 2;
        /// <summary>
        /// максимальна кількість сусідів для виживания
        /// </summary>
        int MaxNeighbors = 4;
        /// <summary>
        /// кількість сусідів, при якому в мертвій клітинці зароджується життя
        /// </summary>
        int NeighborsBornNew = 3;
        /// <summary>
        /// Ризик раптової смерті %
        /// </summary>
        int SuddenDeathPercent = 7;
        /// <summary>
        /// максимальний час життя клітки
        /// </summary>
        int MaxTTL = 6;
        /// <summary>
        /// масив значень поточного часу життя
        /// </summary>
        int[,] LifeTime;
        ///Признак того, що стан теперешній стан буде змінений 
        bool Changed = false;
        /// <summary>
        /// пауза
        /// </summary>
        private static bool suspended = true;
        /// <summary>
        /// ознака одного кроку трансформації
        /// </summary>
        private static bool OneStep = false;
        /// <summary>
        /// Количество отображаемых точек графика, может регулироваться во время работы
        /// </summary>
        private static int ChartPoints = 0;
        /// <summary>
        /// Максимальное количество отображаемых точек графика
        /// </summary>
        private static int MaxChartPoints = 0;
        /// <summary>
        /// Максимальное значение живых клеток
        /// </summary>
        private static int MaxAlive = 0;
        /// <summary>
        /// Минимальное значение живых клеток
        /// </summary>
        private static int MinAlive = 0;
        /// <summary>
        /// Максимальное значение родившихся клеток
        /// </summary>
        private static int MaxBorn = 0;
        /// <summary>
        /// Минимальное значение родившихся клеток
        /// </summary>
        private static int MinBorn = 0;
        /// <summary>
        /// Максимальное значение умерших клеток
        /// </summary>
        private static int MaxDead = 0;
        /// <summary>
        /// Минимальное значение умерших клеток
        /// </summary>
        private static int MinDead = 0;
        /// <summary>
        /// Флаг признака начального заполнения при старте программы
        /// </summary>
        private static bool seedcomplete = false;
        /// <summary>
        /// Аксессор для экземпляра формы
        /// </summary>
        private MainForm Instance;
        /// <summary>
        /// Высота поля
        /// </summary>
        private static int FieldHeight = 0;
        /// <summary>
        /// Ширина поля
        /// </summary>
        private static int FieldWidth = 0;
        /// <summary>
        /// Номер хода
        /// </summary>
        private static int movenumber = 0;
        /// <summary>
        /// Плотность живых клеток при начальном заполнении
        /// </summary>
        private int InitialDensityPercent = 0;
        /// <summary>
        /// Двумерный массив текущего состояния
        /// </summary>
        private bool[,] CurrentState = new bool[FieldHeight, FieldWidth];
        /// <summary>
        /// Двумерный массив состояния на следующем ходу
        /// </summary>
        private bool[,] NextState = new bool[FieldHeight, FieldWidth];
        /// <summary>
        /// ОБъект картинки, изображающей поле
        /// </summary>
        DirectBitmap CurrentBitMap;
        /// <summary>
        /// кратность увеличения
        /// </summary>
        int zoomFactor = 1;

        /// <summary>
        /// Источник сигнала об окончании работы для асинхронных задач обработки трансформации игрового поля
        /// </summary>
        private static CancellationTokenSource CTS;

        /// <summary>
        /// Объект для запоминания последней отрисованной точки при рисовании мышкой
        /// </summary>
        Point lastPoint = Point.Empty;

        /// <summary>
        /// Переменная которая хранит признак нажатой кнопки мыши (для рисования)
        /// </summary>
        bool isMouseDown = new Boolean();

        /// <summary>
        /// Переменная хранит признак выполняющихся вычислений
        /// </summary>
        bool isBusy = false;

        /// <summary>
        /// Запуск приложения
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            // инициализируем переменные
            Instance = this;
            MaxTTL = (int)ttl.Value;
            SuddenDeathPercent = (int)sdprob.Value;
            FieldHeight = pic.Height;
            FieldWidth = pic.Width;
            LifeTime = new int[FieldHeight, FieldWidth];
            CurrentBitMap = new DirectBitmap(FieldWidth, FieldHeight);
            //zoomcb.SelectedIndex = 0;
            cpcb.SelectedIndex = 0;
            pic.MouseWheel += Pic_MouseWheel;
            ChartPoints = Convert.ToInt16(cpcb.Items[0].ToString());
            // подключаем обработчик события изменения положения ползунка (если подключить раньше или в свойствах контрола, будет 
            // ошибка на две строки ранее при изменении индекса)
            cpcb.SelectedIndexChanged += cpcb_SelectedIndexChanged;
            // Выбираем максимальное значение, доступное в выпаджающем списке (должен быть отсортирован по возрастанию)
            MaxChartPoints = Convert.ToInt32(cpcb.Items[cpcb.Items.Count - 1].ToString());

            // запускаем асинхронную задачу пошаговой трансформации поля в фоновом потоке
            Task.Run(() => {
                while (true)
                {
                    // если флаг паузы выставлен в true, ждем и проверяем через каждые 100мс
                    while (suspended)
                    {
                        Task.Delay(100);
                    }
                    try
                    {
                        DrawBitmap();
                        Transform();
                        CheckAndStoreHIstoryHash();
                    }
                    catch (Exception ex)
                    {
                        Instance.Invoke((MethodInvoker)delegate// делегируем отрисовку GUI основному потоку, в котором обрабатывается
                        // весь интерфейс
                        {
                            MessageBox.Show(Instance, string.Format("Ошибка на шаге {0}: {1}{2}{3}", movenumber, ex.Message, 
                                Environment.NewLine, ex.InnerException?.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    movenumber++;
                    // проверяем признак пошагового выполнения
                    if (OneStep)
                    {
                        // снимаем флаг пошагового выполнения
                        OneStep = false;
                        Suspend();
                    }
                }
            });
        }

        /// <summary>
        /// зум картинки меняется в пределах х1..х4 прокручиванием колесика мыши, на 4 Гб ОЗУ больше 4 ставить не надо памяти не хватит.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pic_MouseWheel(object sender, MouseEventArgs e)
        {
            Instance.Invoke((MethodInvoker)delegate
            {
                bool show = false;
                if (e.Delta > 3)
                {
                    if (zoomFactor < 5) zoomFactor++;
                    if (suspended)
                    {
                        show = true;
                    }
                }
                if (e.Delta < -3)
                {
                    if (zoomFactor > 1) zoomFactor--;
                    if (suspended)
                    {
                        show = true;
                    }
                }
                if (show) ShowBitMap();
            });
        }

        /// <summary>
        /// Выводим картинку с текущей степенью увеличения
        /// </summary>
        private void ShowBitMap()
        {
            Size newSize = new Size((int)(CurrentBitMap.Width * zoomFactor), (int)(CurrentBitMap.Height * zoomFactor));
            Bitmap tmpbitmap = new Bitmap(CurrentBitMap.Bitmap, newSize);
            pic.Width = tmpbitmap.Width;
            pic.Height = tmpbitmap.Height;
            pic.Image = tmpbitmap;
            pic.Refresh();
            panel1.VerticalScroll.Visible = (pic.Height > panel1.Height);
            panel1.HorizontalScroll.Visible = (pic.Width > panel1.Width);
            zoomlabel.Text = string.Format("x{0}", zoomFactor);
            Refresh();
        }

        /// <summary>
        /// Заполнение поля (задание начальных условий)
        /// </summary>
        private void InitialSeed()
        {
            int alive = 0;
            MaxAlive = 0;
            MinAlive = 0;
            if (CurrentState.Length == 0)
                CurrentState = new bool[FieldHeight, FieldWidth];
            if (InitialDensityPercent > 0)
            {
                Random rnd = new Random(Guid.NewGuid().GetHashCode());
                for (int y = 0; y < FieldHeight; y++)
                {
                    for (int x = 0; x < FieldWidth; x++)
                    {
                        CurrentState[y, x] = (rnd.Next(100) <= InitialDensityPercent);
                        if (CurrentState[y, x])
                        {
                            alive++;
                            MaxAlive++;
                            MinAlive++;
                        }
                        LifeTime[y, x] = -1;
                    }
                }
            } else
            {// если процент случайного заполнения = 0, рисуем черный квадрат
                CurrentState = new bool[FieldHeight, FieldWidth];
                for (int y = 0; y < FieldHeight; y++)
                {
                    for (int x = 0; x < FieldWidth; x++)
                    {

                        LifeTime[y, x] = -1;
                    }
                }
            }
            MaxBorn = 0;
            MinBorn = 999999999;
            MaxDead = 0;
            MinDead = 999999999;
            movenumber = 0;
            ShowCurrentStepInfo();
            DrawBitmap();
            if (InitialDensityPercent == 0)
            {
                RunButton.Enabled = false;
                RunOneStepButton.Enabled = false;
                ResetButton.Enabled = false;
                seedcomplete = false;
            }
            else
            {
                if (!seedcomplete)
                {
                    RunButton.Enabled = true;
                    RunOneStepButton.Enabled = true;
                    ResetButton.Enabled = true;
                    seedcomplete = true;
                }
            }
        }

        /// <summary>
        /// Выводим показания счетчиков и номер шага на форму
        /// </summary>
        private void ShowCurrentStepInfo()
        {
            Instance.Invoke((MethodInvoker)delegate// делегируем отрисовку GUI основному потоку, в котором обрабатывается
            {
                movelabel.Text = string.Format("Move# {0}", movenumber);
                bornlabel.Text = string.Format("Born: {0} (Max: {1}/Min: {2})", born, MaxBorn, MinBorn);
                deadlabel.Text = string.Format("Dead: {0} (Max: {1}/Min: {2})", dead, MaxDead, MinDead);
                alivelabel.Text = string.Format("Alive: {0} (Max: {1}/Min: {2})", alive, MaxAlive, MinAlive);
                Refresh();
            });
        }

        /// <summary>
        /// Трансформация текущего состояния по правилам
        /// </summary>
        private void Transform()
        {
            born = 0;
            dead = 0;
            alive = 0;
            Changed = false;
            NextState = new bool[FieldHeight, FieldWidth];

            // обсчитывать трансформацию будем в два параллельных потока
            // создаем массив асинхронных задач, делим поле пополам
            int divider = (int)(FieldHeight / 2);

            CTS = new CancellationTokenSource();// сброс сигнала об остановке асинхронных задач


            var task1 = Task.Run(() => ProcessTransform(0, divider), CTS.Token);
            var task2 = Task.Run(() => ProcessTransform(divider, FieldHeight), CTS.Token);
            Task.WhenAll(task1, task2).Wait();// ожидаемся конца выполнения всех потоков

            Instance.Invoke((MethodInvoker)delegate// делегируем отрисовку GUI основному потоку, в котором обрабатывается весь интерфейс
            {
                // обновляем максимальные и минимальные значения
                if (alive > MaxAlive) MaxAlive = alive;
                if (alive < MinAlive) MinAlive = alive;
                if (born > MaxBorn) MaxBorn = born;
                if (born < MinBorn) MinBorn = born;
                if (dead > MaxDead) MaxDead = dead;
                if (dead < MinDead) MinDead = dead;

                ShowCurrentStepInfo();

                // если превышено максимальное количество отображаемых точек графика, удаляем "лишние" точки из начала коллекции,
                // оставляем последние в количестве = MaxChartPoints
                if (chart.Series[0].Points.Count > MaxChartPoints)
                {
                    for (int i = 0; i < chart.Series[0].Points.Count - MaxChartPoints; i++)
                    {
                        chart.Series[0].Points.RemoveAt(0);
                        chart.Series[1].Points.RemoveAt(0);
                        chart.Series[2].Points.RemoveAt(0);
                    }
                }
                // Устанавливаем максимальное и минимальное значение по оси X, в зависимости от того, сколько точек выбрано для отображения
                // и номера текущего шага
                chart.ChartAreas[0].AxisX.Minimum = (movenumber <= ChartPoints) ? 0 : (movenumber - ChartPoints);
                chart.ChartAreas[0].AxisX.Maximum = (movenumber <= ChartPoints) ? ChartPoints : (chart.ChartAreas[0].AxisX.Minimum + ChartPoints);
                // Меняем масштаб графика по оси Y, в зависимости от максимального отображаемого значения
                chart.ChartAreas[0].AxisY.Minimum = (movenumber <= ChartPoints) ? Math.Floor(Math.Min(MinAlive, Math.Min(MinDead, MinBorn)) * 0.5)
                : Math.Floor(Math.Min(chart.Series[1].Points.Reverse().Take(ChartPoints).Reverse().Min(a => a.YValues[0]), chart.Series[2].Points.Reverse()
                .Take(ChartPoints).Reverse().Min(b => b.YValues[0])) * 0.5);
                chart.ChartAreas[0].AxisY.Maximum = (movenumber <= ChartPoints) ? Math.Floor(Math.Max(MaxAlive, Math.Max(MaxDead, MaxBorn)) * 1.1)
                : Math.Floor(chart.Series[0].Points.Reverse().Take(ChartPoints).Reverse().Max(a => a.YValues[0]) * 1.1);
                // Добавляем точки на графики количества живых, умерших и родившихся клеток на текущем шаге
                chart.Series[0].Points.AddXY(movenumber, alive);
                chart.Series[1].Points.AddXY(movenumber, born);
                chart.Series[2].Points.AddXY(movenumber, dead);
            });
            if (Changed)
            {
                // Есть изменения, копируем состояние следующего шага в текущий массив
                Array.Copy(NextState, CurrentState, NextState.Length);
            }
        }

        /// <summary>
        /// Задача для обсчета трансформации игрового поля для следующего шага в фоновом потоке
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private async Task<bool> ProcessTransform(int from, int to)
        {
            await Task.Run(() =>
            {
                Random sdrnd = new Random(Guid.NewGuid().GetHashCode());// Инициализируем генератор случайных чисел
                for (int y = from; y < to; y++)
                {
                    for (int x = 0; x < FieldWidth; x++)
                    {
                        int ncount = GetNeighborsMask(y, x);// получаем количество соседей из байта состояния
                        bool state = CurrentState[y, x];// текущее состояние клетки: true = жива
                        switch (state)
                        {
                            case true:// клетка жива на текущем шаге
                                LifeTime[y, x]++;
                                alive++;

                                // риск внезапной смерти растет с возрастом клетки, от половины заданного
                                // значения в начале жизни до 100% его значения в конце (если задан максимальный срок). Если не задан, риск внезапной
                                // смерти постоянный на протяжении всей жизни клетки и равен заданному значению SuddenDeathPercent
                                var CurrentSuddenDeathRisk = SuddenDeathPercent > 0 ? SuddenDeathPercent * (MaxTTL > 0 ? (0.5 + LifeTime[y, x]
                                / (2 * MaxTTL)) : 1) * 1.00 : 0;

                                // проверяем смертельные факторы, если хоть один сработал - клетке пиздец
                                // TODO: назначить старым клеткам пенсию и поиграть с ее выдачей и пенсионным возрастом
                                if (ncount > MaxNeighbors// перенаселённость
                                || ncount < MinNeighbors// одиночество
                                || (LifeTime[y, x] == MaxTTL)// старость
                                || (sdrnd.NextDouble() * 100 <= CurrentSuddenDeathRisk))// Внезапная (случайная) смерть
                                {
                                    // клетка умирает
                                    dead++;
                                    alive--;
                                    NextState[y, x] = false;
                                    LifeTime[y, x] = 0;
                                }
                                else
                                {
                                    // клетка продолжает жить
                                    NextState[y, x] = true;
                                }
                                break;

                            case false:// клетка мертва на текущем шаге
                                if (!state && ncount == NeighborsBornNew)
                                {
                                    // в пустой (мёртвой) клетке, рядом с которой определенное количество живых клеток, зарождается жизнь
                                    // TODO: назначить окружающим клеткам выплату материнского капитала, ускорить старение "многодетных" клеток и т. д...
                                    NextState[y, x] = true;
                                    born++;
                                    LifeTime[y, x] = 0;
                                }
                                else
                                {
                                    // клетка остается мертвой
                                    NextState[y, x] = false;
                                    LifeTime[y, x] = -1;
                                }
                                break;
                        }
                        // Вычисляем есть ли разница между текущим и следующим шагом с помощью логического ИЛИ
                        // (достаточно одного изменения в массиве, чтобы итоговое значение было Changed == true)
                        Changed |= CurrentState[y, x] != NextState[y, x];
                    }
                }
            });
            return true;
        }

        /// <summary>
        /// Находит соседей указанной клетки и возвращает их количество
        /// </summary>
        /// <param name="y">координата y указанной клетки</param>
        /// <param name="x">координата x указанной клетки</param>
        /// <returns>1 байт с битовой маской соседей указанной клетки, начинай с левой верхней</returns>
        private int GetNeighborsMask(int y, int x)
        {
            int res = 0;
            //Противоположные края поля замкнуты друг на друга, т.е.при переходе через границы точка уходит на противоположную сторону картинки
            var Yinc = (y + 1) % FieldHeight;//инкрементированное значение координаты Y
            var Ydec = (FieldHeight + y - 1) % FieldHeight;//декрементированное значение координаты Y
            var Xinc = (x + 1) % FieldWidth;//инкрементированное значение координаты X
            var Xdec = (FieldWidth + x - 1) % FieldWidth;//декрементированное значение координаты X
            if (CurrentState[Yinc, Xdec]) res++;// проворяем клетку выше и левее
            if (CurrentState[Yinc, (x)]) res++;// проворяем клетку выше
            if (CurrentState[Yinc, Xinc]) res++;// проворяем клетку выше и правее
            if (CurrentState[(y), Xdec]) res++;// проворяем клетку левее
            if (CurrentState[(y), Xinc]) res++;// проворяем клетку правее
            if (CurrentState[Ydec, Xdec]) res++;// проворяем клетку ниже и левее
            if (CurrentState[Ydec, (x)]) res++;// проворяем клетку ниже
            if (CurrentState[Ydec, Xinc]) res++;// проворяем клетку ниже и правее
            return res;
        }

        /// <summary>
        /// Формируем картинку из объекта текущего состояния
        /// </summary>
        private void DrawBitmap()
        {
            //делаем инвок для синхронизации доступа в поток GUI
            Instance.Invoke((MethodInvoker)delegate
            {
                for (int y = 0; y < CurrentBitMap.Height; y++)
                {
                    for (int x = 0; x < CurrentBitMap.Width; x++)
                    {
                        // устанавливаем цвет клетки
                        CurrentBitMap.SetPixel(x, y, 
                            CurrentState[y, x]// проверяем живая или мертвая
                            ? (LifeTime[y, x] == 0 // проверем возраст живой
                            ? Color.FromArgb(255, 0, 0, 255)// новорожденные красим синим
                            : Color.FromArgb(255, 0, 255, 0))// взрослые красим зеленым
                            : (LifeTime[y, x] == 0 // проверем возраст мертвой
                            ? Color.FromArgb(255, 255, 0, 0)// умершие красим синим
                            : Color.FromArgb(255, 0, 0, 0)));// сгнившие красим черным
                    }
                }
                ShowBitMap();
            });
        }

        /// <summary>
        /// Нажатие на кнопку Пуск
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            Play();
        }

        /// <summary>
        /// нажатие на кнопку пауза
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            Suspend();
        }

        /// <summary>
        /// Снимаем с паузы \ запускаем
        /// </summary>
        private void Play()
        {
            Instance.Invoke((MethodInvoker)delegate// делегируем отрисовку GUI основному потоку, в котором обрабатывается весь интерфейс
            {
                //запускаем симуляцию, делаем недоступными кнопки "пуск" и "пошаговое выполнения" и ползунок, разблокируем кнопку "пауза"
                PauseButton.Enabled = !OneStep;
                RunButton.Enabled = false;
                RunOneStepButton.Enabled = false;
                FillingPercentileTracker.Enabled = false;
                ResetButton.Enabled = false;
                //снимаем флаг паузы
                suspended = false;
            });
        }

        /// <summary>
        /// Ставим на паузу
        /// </summary>
        private void Suspend()
        {
            Instance.Invoke((MethodInvoker)delegate// делегируем отрисовку GUI основному потоку, в котором обрабатывается весь интерфейс
            {
                //останавливаем симуляцию, делаем доступными кнопки "пуск" и "пошаговое выполнения" и ползунок, блокируем кнопку "пауза"
                PauseButton.Enabled = false;
                RunButton.Enabled = true;
                RunOneStepButton.Enabled = true;
                FillingPercentileTracker.Enabled = true;
                ResetButton.Enabled = true;
                //устанавливаем флаг паузы
                suspended = true;
            });
        }

        /// <summary>
        /// Движение ползунка приводит к заполнению поля с указанным процентом живых клеток
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            ClearChart();
            InitialDensityPercent = FillingPercentileTracker.Value;
            seedpercent.Text = InitialDensityPercent.ToString();
            CurrentState = new bool[FieldHeight, FieldWidth];
            NextState = new bool[FieldHeight, FieldWidth];
            InitialSeed();
        }

        /// <summary>
        /// очистка графика
        /// </summary>
        private void ClearChart()
        {
            Instance.Invoke((MethodInvoker)delegate// делегируем отрисовку GUI основному потоку, в котором обрабатывается весь интерфейс
            {
                chart.Series[0].Points.Clear();
                chart.Series[1].Points.Clear();
                chart.Series[2].Points.Clear();
            });
        }

        /// <summary>
        /// Нажатие кнопки пошагового выполнения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click_1(object sender, EventArgs e)
        {
            OneStep = true;
            Play();
        }

        /// <summary>
        /// Изменение числа отображаемых точек приводит к изменению масштаба графика
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cpcb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChartPoints = Convert.ToInt16(cpcb.Text);
            // Синхронизируем доступ к GUI
            Instance.Invoke((MethodInvoker)delegate
            {
                chart.ChartAreas[0].AxisX.Minimum = (movenumber <= ChartPoints) ? 0 : (movenumber - chart.Series[0].Points.Count);
                chart.ChartAreas[0].AxisX.Maximum = (movenumber <= ChartPoints) ? ChartPoints : (chart.ChartAreas[0].AxisX.Minimum
                + ChartPoints);
            });
        }

        /// <summary>
        /// меняем значение риска внезапной смерти
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sdprob_ValueChanged(object sender, EventArgs e)
        {
            SuddenDeathPercent = (int)sdprob.Value;
        }

        /// <summary>
        /// меняем значение максимальной продолжительности жизни
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ttl_ValueChanged(object sender, EventArgs e)
        {
            MaxTTL = (int)ttl.Value;
        }

        /// <summary>
        /// Изменение минимального для выживания количества соседей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nmin_ValueChanged(object sender, EventArgs e)
        {
            MinNeighbors = (int)nmin.Value;
        }

        /// <summary>
        /// Изменение максимеального для выживания количества соседей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nmax_ValueChanged(object sender, EventArgs e)
        {
            MaxNeighbors = (int)nmax.Value;
        }

        /// <summary>
        /// Изменение количества клеток для рождения новой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            NeighborsBornNew = (int)newborn.Value;
        }

        /// <summary>
        /// Двигаем мышкой с зажатой кнопкой, рисуем линию на картинке
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pic_MouseMove(object sender, MouseEventArgs e)
        {
            if (!suspended || zoomFactor != 1 || isBusy)
                return;// рисовать будем только если нет увеличения и на паузе
            if (isMouseDown == true)
            {
                if (lastPoint != null)
                {
                    using (Graphics g = Graphics.FromImage(pic.Image))
                    {
                        g.DrawLine(new Pen(Color.FromArgb(255, 0, 255, 0), 1), lastPoint, e.Location);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                    }
                    pic.Invalidate();
                    lastPoint = e.Location;
                }
            }
        }

        /// <summary>
        /// Нажата кнопка мыши над картинкой, начинаем рисовать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pic_MouseDown(object sender, MouseEventArgs e)
        {
            if (!suspended || zoomFactor != 1 || isBusy)
                return;// рисовать будем только если нет увеличения и на паузе
            lastPoint = e.Location;
            isMouseDown = true;
        }

        /// <summary>
        /// Кнопка мыши отпущена, заканчиваем отрисовку и копируем картинку в объект текущего состояния
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pic_MouseUp(object sender, MouseEventArgs e)
        {
            if (!suspended || zoomFactor != 1 || isBusy)
                return;// рисовать будем только если нет увеличения и на паузе
            isBusy = true;
            Task.Run(() =>
            {
                Instance.Invoke((MethodInvoker)delegate
                {
                    var bm = (Bitmap)pic.Image;
                    isMouseDown = false;
                    lastPoint = Point.Empty;
                    for (int y = 0; y < pic.Height; y++)
                    {
                        for (int x = 0; x < pic.Width; x++)
                        {
                            CurrentState[y, x] = (bm.GetPixel(x, y) != Color.FromArgb(255, 0, 0, 0));
                        }
                    }
                    if (!RunButton.Enabled)
                    {
                        RunButton.Enabled = true;
                        RunOneStepButton.Enabled = true;
                        ResetButton.Enabled = true;
                    }
                });
                isBusy = false;
            });
        }

        /// <summary>
        /// Окно программы показывается первый раз
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            InitialSeed();
        }

        /// <summary>
        /// Нажатие на кнопку Сброс
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, EventArgs e)
        {
            zoomFactor = 1;
            ClearChart();
            InitialSeed();
            HistoryHashes.Clear();
        }

        private void CheckAndStoreHIstoryHash()
        {
            // ограничим отслеживание истории списком состояний длиной 10000 записей, чтобы не терять производительность
            if (HistoryHashes.Count == 10000)
                HistoryHashes.RemoveAt(0);
            byte[] tmparr;
            using (var memoryStream = new MemoryStream())
            {
                CurrentBitMap.Bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                tmparr = memoryStream.ToArray();
            }
            // вычисляем хэш-функцию от текущего состояния игрового поля, длиной 256 бит = 32 байта
            SHA256 shaM = new SHA256Managed();
            var HashedbytesString = Convert.ToBase64String(shaM.ComputeHash(tmparr));
            if (!HistoryHashes.Any(rec => rec == HashedbytesString))
            {
                HistoryHashes.Add(HashedbytesString);// строки в истории еще нет => текущее состояние уникально, продолжаем
            }
            else
            {// строк анайдена в списке => текущее состояние повторяет предыдущее на одном из шагов => гейм овер
                var stepsbefore = HistoryHashes.Count - HistoryHashes.IndexOf(HashedbytesString);// сколько шагов назад было совпадение
                HistoryHashes.Clear();// чистим историю
                Instance.Invoke((MethodInvoker)delegate// делегируем отрисовку GUI основному потоку, в котором обрабатывается весь интерфейс
                {
                    CTS.Cancel();// подаем сигнал СТОП асинхронным задачам
                    button2_Click(null, null);// нажимаем кнопку "Пауза"
                    MessageBox.Show(Instance, string.Format("Конфигурация поколения {0} повторяет конфигурацию поколений назад: {1}", movenumber, stepsbefore), "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
        }

        private void pic_Click(object sender, EventArgs e)
        {

        }
    }
}
